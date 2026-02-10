using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Services;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public partial class ModelsViewModel : ObservableObject
{
    private readonly ObservableCollection<ModelItemViewModel> _allModels = new();
    private readonly ModelFilterService _filterService;
    private readonly ProviderService _providerService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InputPriceHeader))]
    [NotifyPropertyChangedFor(nameof(OutputPriceHeader))]
    private string _currencySymbol = "$";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ModelsCountText))]
    private ObservableCollection<ModelItemViewModel> _filteredModels = new();

    [ObservableProperty] private ObservableCollection<ProviderGroupViewModel> _groupedModels = new();
    [ObservableProperty] private bool _isAudioSelected = true;
    [ObservableProperty] private bool _isEmbeddingsSelected = true;
    [ObservableProperty] private bool _isFileSelected = true;
    [ObservableProperty] private bool _isImageSelected = true;

    [ObservableProperty] private bool _isTextSelected = true;
    [ObservableProperty] private bool _isVideoSelected = true;
    [ObservableProperty] private int _maxPriceIndex = 6;

    [ObservableProperty] private int _minPriceIndex;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showFavoritesOnly;

    [ObservableProperty] private ModelSortOption _sortOption = ModelSortOption.NameAsc;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ViewModeIndex))]
    private ModelsViewMode _viewMode = ModelsViewMode.List;

    public ModelsViewModel(ProviderService providerService, ModelFilterService filterService)
    {
        _providerService = providerService;
        _filterService = filterService;
        LoadData();

        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this,
            (r, m) => { App.MainWindow.DispatcherQueue.TryEnqueue(ApplyFilters); });

        WeakReferenceMessenger.Default.Register<ProviderUpdatedMessage>(this, (r, m) =>
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                var group = GroupedModels.FirstOrDefault(g => g.ProviderId == m.ProviderId);
                if (group != null)
                {
                    var provider = _providerService.GetAllProviders().FirstOrDefault(p => p.Id == m.ProviderId);
                    if (provider != null)
                        group.ProviderIcon = provider.IconPath;
                }
            });
        });

        UpdateCurrencySymbol();
    }

    public string InputPriceHeader =>
        string.Format(ResourceLoader.GetForViewIndependentUse("Models").GetString("InputPriceFormat"), CurrencySymbol);

    public string OutputPriceHeader =>
        string.Format(ResourceLoader.GetForViewIndependentUse("Models").GetString("OutputPriceFormat"), CurrencySymbol);

    public string ModelsCountText => $"{FilteredModels.Count} models";

    public int ViewModeIndex
    {
        get => ViewMode == ModelsViewMode.List ? 0 : 1;
        set => ViewMode = value == 1 ? ModelsViewMode.Card : ModelsViewMode.List;
    }

    partial void OnViewModeChanged(ModelsViewMode value)
    {
        OnPropertyChanged(nameof(ViewModeIndex));
    }
}

public enum ModelsViewMode
{
    List,
    Card
}