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
        query = SelectedSortOption switch
        {
            "Name" => query.OrderBy(m => m.DisplayName),
            "Price" => query.OrderBy(m => m.ConvertedInputPrice.HasValue ? 0 : 1)
                .ThenBy(m => m.ConvertedInputPrice ?? m.InputPrice ?? decimal.MaxValue),
            _ => query.OrderBy(m => m.ProviderName).ThenBy(m => m.DisplayName) // Default: Provider
        };

        FilteredModels = new ObservableCollection<ModelItemViewModel>(query);
    }
}