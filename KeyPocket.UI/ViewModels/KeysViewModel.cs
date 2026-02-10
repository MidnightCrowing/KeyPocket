using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Services;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public partial class KeysViewModel : ObservableObject
{
    private readonly ObservableCollection<KeyItemViewModel> _allKeys = new();
    private readonly ProviderService _providerService;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(KeysCountText))]
    private ObservableCollection<KeyItemViewModel> _filteredKeys = new();

    [ObservableProperty] private ObservableCollection<KeyProviderGroupViewModel> _groupedKeys = new();

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private bool _showFavoritesOnly;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ViewModeIndex))]
    private KeysViewMode _viewMode = KeysViewMode.List;

    public KeysViewModel(ProviderService providerService)
    {
        _providerService = providerService;
        LoadData();

        // Register for theme changes to update icons (if needed)
        // ModelsViewModel does this. KeyItemViewModel might need it? 
        // KeyItemViewModel takes providerIcon string. ProviderGroupViewModel uses Helper to get Uri.
        // KeyProviderGroupViewModel also uses Helper. So we might need to refresh on theme change.
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                // Refresh icons in groups
                foreach (var group in GroupedKeys) group.RefreshIcon();
            });
        });
    }

    public string KeysCountText => $"{FilteredKeys.Count} keys";

    public int ViewModeIndex
    {
        get => ViewMode == KeysViewMode.List ? 0 : 1;
        set => ViewMode = value == 1 ? KeysViewMode.Card : KeysViewMode.List;
    }

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

    partial void OnViewModeChanged(KeysViewMode value)
    {
        OnPropertyChanged(nameof(ViewModeIndex));
    }

    private void ApplyFilters()
    {
        var query = _allKeys.AsEnumerable();

        // 1. Search Text (Search Tag only)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var lowerSearch = SearchText.ToLowerInvariant();
            query = query.Where(k =>
                k.Tag != null && k.Tag.ToLowerInvariant().Contains(lowerSearch));
        }

        // 2. Favorites
        if (ShowFavoritesOnly) query = query.Where(k => k.IsFavorite);

        // 3. Sorting (Default: Newest first)
        query = query.OrderByDescending(k => k.CreatedAt);

        var result = query.ToList();
        FilteredKeys = new ObservableCollection<KeyItemViewModel>(result);

        GenerateGroupedKeys(result);
    }

    private void GenerateGroupedKeys(List<KeyItemViewModel> keys)
    {
        var providers = _providerService.GetAllProviders();
        var providerOrderMap = providers
            .Select((p, index) => new { p.Name, Index = index })
            .ToDictionary(x => x.Name, x => x.Index);

        var grouped = keys
            .GroupBy(k => new
            {
                k.ProviderName, k.ProviderIcon
            }) // KeyItemViewModel needs ProviderIcon public property? Checked: it has _providerIcon backing field, but only constructor param. 
            // Wait, KeyItemViewModel.cs verification: 
            // [ObservableProperty] private string? _providerIcon; -> This generates Public ProviderIcon property. So it is fine.
            .OrderBy(g => providerOrderMap.TryGetValue(g.Key.ProviderName, out var index) ? index : int.MaxValue)
            .Select(g => new KeyProviderGroupViewModel
            {
                ProviderId = providers.FirstOrDefault(p => p.Name == g.Key.ProviderName)?.Id ?? Guid.Empty,
                ProviderName = g.Key.ProviderName,
                ProviderIcon = g.Key.ProviderIcon,
                Keys = g.ToList()
            })
            .ToList();

        GroupedKeys = new ObservableCollection<KeyProviderGroupViewModel>(grouped);
    }
}

public enum KeysViewMode
{
    List,
    Card
}