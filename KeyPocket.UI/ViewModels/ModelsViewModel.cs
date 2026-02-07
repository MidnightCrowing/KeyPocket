using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Services;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public partial class ModelsViewModel : ObservableObject
{
    private readonly ObservableCollection<ModelItemViewModel> _allModels = new();
    private readonly ProviderService _providerService;
    private readonly ModelFilterService _filterService;

    [ObservableProperty] private string _currencySymbol = "$";

    [ObservableProperty] private ObservableCollection<ModelItemViewModel> _filteredModels = new();

    [ObservableProperty] private ObservableCollection<ProviderGroupViewModel> _groupedModels = new();

    [ObservableProperty] private string _searchText = string.Empty;

    // Capability 多选属性(默认全选)
    [ObservableProperty] private bool _isTextSelected = true;
    [ObservableProperty] private bool _isFileSelected = true;
    [ObservableProperty] private bool _isImageSelected = true;
    [ObservableProperty] private bool _isAudioSelected = true;
    [ObservableProperty] private bool _isVideoSelected = true;
    [ObservableProperty] private bool _isEmbeddingsSelected = true;

    // 价格范围索引(非线性刻度: Free=0, 0.1=1, 0.2=2, 0.5=3, 1=4, 5=5, 10+=6)
    [ObservableProperty] private int _minPriceIndex = 0;
    [ObservableProperty] private int _maxPriceIndex = 6;

    // 排序选项
    [ObservableProperty] private ModelSortOption _sortOption = ModelSortOption.NameAsc;

    [ObservableProperty] private bool _showFavoritesOnly;

    public ModelsViewModel(ProviderService providerService, ModelFilterService filterService)
    {
        _providerService = providerService;
        _filterService = filterService;
        LoadData();

        // Register for theme changes to update icons
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(ApplyFilters);
        });

        UpdateCurrencySymbol();
    }

    private void UpdateCurrencySymbol()
    {
        var currency = Helpers.SettingsHelper.Current.SelectedCurrency;
        var symbols = Helpers.SettingsHelper.Current.CurrencySymbols;
        CurrencySymbol = symbols.TryGetValue(currency, out var symbol) ? symbol : currency;
    }



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

        // 显式触发一次属性更改通知，确保 UI 同步（解决 Capability 打开时未选中的问题）
        OnPropertyChanged(nameof(IsTextSelected));
        OnPropertyChanged(nameof(IsFileSelected));
        OnPropertyChanged(nameof(IsImageSelected));
        OnPropertyChanged(nameof(IsAudioSelected));
        OnPropertyChanged(nameof(IsVideoSelected));
        OnPropertyChanged(nameof(IsEmbeddingsSelected));

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

    partial void OnIsTextSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsFileSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsImageSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsAudioSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsVideoSelectedChanged(bool value) => ApplyFilters();
    partial void OnIsEmbeddingsSelectedChanged(bool value) => ApplyFilters();
    partial void OnMinPriceIndexChanged(int value) => ApplyFilters();
    partial void OnMaxPriceIndexChanged(int value) => ApplyFilters();
    partial void OnSortOptionChanged(ModelSortOption value) => ApplyFilters();

    private void ApplyFilters()
    {
        // 构建过滤条件
        var criteria = new ModelFilterCriteria
        {
            SearchText = SearchText,
            ShowFavoritesOnly = ShowFavoritesOnly,
            SelectedCapabilities = GetSelectedCapabilities(),
            MinPrice = ConvertPriceIndexToValue(MinPriceIndex),
            MaxPrice = ConvertPriceIndexToValue(MaxPriceIndex),
            SortOption = SortOption
        };

        // 调用 Core 层服务进行过滤
        var allModelsData = _allModels.Select(vm => new Core.Models.ModelInfo
        {
            Id = vm.Id,
            DisplayName = vm.DisplayName,
            IsFavorite = vm.IsFavorite,
            IsChatModel = vm.IsChatModel,
            IsEmbeddingModel = vm.IsEmbeddingModel,
            InputPricePerMTokens = vm.InputPrice
        }).ToList();

        var filtered = _filterService.ApplyFilters(allModelsData, criteria);

        // 将过滤结果映射回 ViewModel
        var filteredIds = filtered.Select(m => m.Id).ToHashSet();
        var filteredVMs = _allModels.Where(vm => filteredIds.Contains(vm.Id));

        // 应用排序(Core 层已排序,保持顺序)
        var orderedVMs = filtered.Select(m => _allModels.First(vm => vm.Id == m.Id)).ToList();

        FilteredModels = new ObservableCollection<ModelItemViewModel>(orderedVMs);

        // 生成分组数据
        GenerateGroupedModels(orderedVMs);
    }

    private List<string> GetSelectedCapabilities()
    {
        var list = new List<string>();
        if (IsTextSelected) list.Add("Text");
        if (IsFileSelected) list.Add("File");
        if (IsImageSelected) list.Add("Image");
        if (IsAudioSelected) list.Add("Audio");
        if (IsVideoSelected) list.Add("Video");
        if (IsEmbeddingsSelected) list.Add("Embeddings");
        return list;
    }

    /// <summary>
    /// 将价格索引转换为实际价格值
    /// 索引映射: 0=Free(0), 1=0.1, 2=0.2, 3=0.5, 4=1, 5=5, 6=10+
    /// </summary>
    private decimal? ConvertPriceIndexToValue(int index)
    {
        return index switch
        {
            0 => 0m,
            1 => 0.1m,
            2 => 0.2m,
            3 => 0.5m,
            4 => 1m,
            5 => 5m,
            6 => null, // 10+ 表示无上限
            _ => null
        };
    }

    private void GenerateGroupedModels(List<ModelItemViewModel> models)
    {
        var providers = _providerService.GetAllProviders();
        var providerOrderMap = providers
            .Select((p, index) => new { p.Name, Index = index })
            .ToDictionary(x => x.Name, x => x.Index);

        var grouped = models
            .GroupBy(m => new { m.ProviderName, m.ProviderIcon })
            .OrderBy(g => providerOrderMap.TryGetValue(g.Key.ProviderName, out var index) ? index : int.MaxValue)
            .Select(g => new ProviderGroupViewModel
            {
                ProviderId = providers.FirstOrDefault(p => p.Name == g.Key.ProviderName)?.Id ?? Guid.Empty,
                ProviderName = g.Key.ProviderName,
                ProviderIcon = g.Key.ProviderIcon,
                Models = g.ToList()
            })
            .ToList();

        GroupedModels = new ObservableCollection<ProviderGroupViewModel>(grouped);
    }



    /// <summary>
    /// 重置所有过滤条件
    /// </summary>
    public void ResetFilters()
    {
        IsTextSelected = true;
        IsFileSelected = true;
        IsImageSelected = true;
        IsAudioSelected = true;
        IsVideoSelected = true;
        IsEmbeddingsSelected = true;
        MinPriceIndex = 0;
        MaxPriceIndex = 6;
        SortOption = ModelSortOption.NameAsc;
    }
}