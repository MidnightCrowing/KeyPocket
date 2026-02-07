using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.Core.Services;

namespace KeyPocket.UI.ViewModels;

public partial class ModelsViewModel : ObservableObject
{
    private readonly ObservableCollection<ModelItemViewModel> _allModels = new();
    private readonly ProviderService _providerService;

    [ObservableProperty] private ObservableCollection<ModelItemViewModel> _filteredModels = new();
    
    [ObservableProperty] private ObservableCollection<ProviderGroupViewModel> _groupedModels = new();

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private string _selectedCapability = "All";

    [ObservableProperty] private string _selectedSortOption = "Provider"; // Provider, Name, Price

    [ObservableProperty] private bool _showFavoritesOnly;

    public ModelsViewModel(ProviderService providerService)
    {
        _providerService = providerService;
        LoadData();
    }

    public ObservableCollection<string> Capabilities { get; } = new()
    {
        "All",
        "Chat",
        "Embedding"
    };

    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "Provider",
        "Name",
        "Price"
    };

    public void LoadData()
    {
        _allModels.Clear();
        var providers = _providerService.GetAllProviders();

        foreach (var provider in providers)
        {
            if (provider.Models == null) continue;

            foreach (var model in provider.Models)
                _allModels.Add(new ModelItemViewModel(model, provider.Name, provider.IconPath, provider.Currency,
                    _providerService));
        }

        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnShowFavoritesOnlyChanged(bool value)
    {
        ApplyFilters();
    }

    partial void OnSelectedCapabilityChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedSortOptionChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var query = _allModels.AsEnumerable();

        // 1. Search Text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var lowerSearch = SearchText.ToLowerInvariant();
            query = query.Where(m =>
                m.DisplayName.ToLowerInvariant().Contains(lowerSearch) ||
                m.Id.ToLowerInvariant().Contains(lowerSearch) ||
                m.ProviderName.ToLowerInvariant().Contains(lowerSearch));
        }

        // 2. Favorites
        if (ShowFavoritesOnly) query = query.Where(m => m.IsFavorite);

        // 3. Capability
        if (SelectedCapability == "Chat")
            query = query.Where(m => m.IsChatModel);
        else if (SelectedCapability == "Embedding") query = query.Where(m => m.IsEmbeddingModel);

        // 4. Sorting
        // This sorting applies to the flat list (FilteredModels)
        query = SelectedSortOption switch
        {
            "Name" => query.OrderBy(m => m.DisplayName),
            "Price" => query.OrderBy(m => m.ConvertedInputPrice.HasValue ? 0 : 1)
                .ThenBy(m => m.ConvertedInputPrice ?? m.InputPrice ?? decimal.MaxValue),
            _ => query.OrderBy(m => m.ProviderName).ThenBy(m => m.DisplayName) // Default: Provider
        };

        FilteredModels = new ObservableCollection<ModelItemViewModel>(query);
        
        // 5. Generate Grouped Data for Tree View
        // Define explicit provider order based on _providerService.GetAllProviders()
        // We can re-fetch or cache. LoadData already fetches, let's rely on _sortedProviders from there if we add it, 
        // or just fetch here since it's cheap (local JSON config).
        var providers = _providerService.GetAllProviders();
        var providerOrderMap = providers
            .Select((p, index) => new { p.Name, Index = index })
            .ToDictionary(x => x.Name, x => x.Index);

        var grouped = query
            .GroupBy(m => new { m.ProviderName, m.ProviderIcon })
            .AsEnumerable() // Switch to client-side eval (safe for lists) to use custom sort
            .OrderBy(g => providerOrderMap.TryGetValue(g.Key.ProviderName, out var index) ? index : int.MaxValue)
            .Select(g => new ProviderGroupViewModel
            {
                ProviderName = g.Key.ProviderName,
                ProviderIcon = g.Key.ProviderIcon,
                // Inner sorting: Alphabetical by DisplayName
                Models = g.OrderBy(m => m.DisplayName).ToList()
            })
            .ToList();
            
        GroupedModels = new ObservableCollection<ProviderGroupViewModel>(grouped);
    }
}