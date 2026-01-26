using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.Core.Services;

namespace KeyPocket.UI.ViewModels;

public partial class KeysViewModel : ObservableObject
{
    private readonly ObservableCollection<KeyItemViewModel> _allKeys = new();
    private readonly ProviderService _providerService;

    [ObservableProperty] private ObservableCollection<KeyItemViewModel> _filteredKeys = new();

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private string _selectedSortOption = "Date Created"; // Date Created, Provider, Status

    [ObservableProperty] private bool _showFavoritesOnly;

    public KeysViewModel(ProviderService providerService)
    {
        _providerService = providerService;
        LoadData();
    }

    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "Date Created",
        "Provider",
        "Status"
    };

    public void LoadData()
    {
        _allKeys.Clear();
        var providers = _providerService.GetAllProviders();

        foreach (var provider in providers)
        {
            if (provider.ApiKeys == null) continue;

            foreach (var key in provider.ApiKeys)
                _allKeys.Add(new KeyItemViewModel(key, provider.Name, provider.IconPath, _providerService));
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

    partial void OnSelectedSortOptionChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var query = _allKeys.AsEnumerable();

        // 1. Search Text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var lowerSearch = SearchText.ToLowerInvariant();
            query = query.Where(k =>
                k.ProviderName.ToLowerInvariant().Contains(lowerSearch) ||
                (k.Tag != null && k.Tag.ToLowerInvariant().Contains(lowerSearch)));
        }

        // 2. Favorites
        if (ShowFavoritesOnly) query = query.Where(k => k.IsFavorite);

        // 3. Sorting
        query = SelectedSortOption switch
        {
            "Provider" => query.OrderBy(k => k.ProviderName),
            "Status" => query.OrderBy(k =>
                k.IsDisabled), // Disabled (true) vs Active (false). Maybe Active first? false < true. So Active first.
            _ => query.OrderByDescending(k => k.CreatedAt) // Default: Date Created (Newest first)
        };

        FilteredKeys = new ObservableCollection<KeyItemViewModel>(query);
    }
}