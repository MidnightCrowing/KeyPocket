using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace KeyPocket.UI.ViewModels;

public partial class ModelsViewModel : ObservableObject
{
    private readonly ProviderService _providerService;
    private readonly ObservableCollection<ModelItemViewModel> _allModels = new();

#pragma warning disable MVVMTK0045
    [ObservableProperty]
    private ObservableCollection<ModelItemViewModel> _filteredModels = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showFavoritesOnly;

    [ObservableProperty]
    private string _selectedCapability = "All"; 

    [ObservableProperty]
    private string _selectedSortOption = "Provider"; // Provider, Name, Price
#pragma warning restore MVVMTK0045

    public ObservableCollection<string> Capabilities { get; } = new ObservableCollection<string>
    {
        "All",
        "Chat",
        "Embedding"
    };

    public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string>
    {
        "Provider",
        "Name",
        "Price"
    };

    public ModelsViewModel(ProviderService providerService)
    {
        _providerService = providerService;
        LoadData();
    }

    public void LoadData()
    {
        _allModels.Clear();
        var providers = _providerService.GetAllProviders();

        foreach (var provider in providers)
        {
            if (provider.Models == null) continue;

            foreach (var model in provider.Models)
            {
                _allModels.Add(new ModelItemViewModel(model, provider.Name, provider.IconPath, _providerService));
            }
        }

        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnShowFavoritesOnlyChanged(bool value) => ApplyFilters();
    partial void OnSelectedCapabilityChanged(string value) => ApplyFilters();
    partial void OnSelectedSortOptionChanged(string value) => ApplyFilters();

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
        if (ShowFavoritesOnly)
        {
            query = query.Where(m => m.IsFavorite);
        }

        // 3. Capability
        if (SelectedCapability == "Chat")
        {
            query = query.Where(m => m.IsChatModel);
        }
        else if (SelectedCapability == "Embedding")
        {
            query = query.Where(m => m.IsEmbeddingModel);
        }

        // 4. Sorting
        query = SelectedSortOption switch
        {
            "Name" => query.OrderBy(m => m.DisplayName),
            "Price" => query.OrderBy(m => m.InputPrice ?? decimal.MaxValue), // Nulls last or treated as expensive? Usually free/unknown. Let's put nulls at end.
            _ => query.OrderBy(m => m.ProviderName).ThenBy(m => m.DisplayName) // Default: Provider
        };

        FilteredModels = new ObservableCollection<ModelItemViewModel>(query);
    }
}
