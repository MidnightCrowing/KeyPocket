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
    public partial string CurrencySymbol { get; set; } = "$";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ModelsCountText))]
    public partial ObservableCollection<ModelItemViewModel> FilteredModels { get; set; } = new();

    [ObservableProperty] public partial ObservableCollection<ProviderGroupViewModel> GroupedModels { get; set; } = new();
    [ObservableProperty] public partial bool IsAudioSelected { get; set; } = true;
    [ObservableProperty] public partial bool IsEmbeddingsSelected { get; set; } = true;
    [ObservableProperty] public partial bool IsFileSelected { get; set; } = true;
    [ObservableProperty] public partial bool IsImageSelected { get; set; } = true;

    [ObservableProperty] public partial bool IsTextSelected { get; set; } = true;
    [ObservableProperty] public partial bool IsVideoSelected { get; set; } = true;
    [ObservableProperty] public partial int MaxPriceIndex { get; set; } = 6;

    [ObservableProperty] public partial int MinPriceIndex { get; set; }
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool ShowFavoritesOnly { get; set; }

    [ObservableProperty] public partial ModelSortOption SortOption { get; set; } = ModelSortOption.NameAsc;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ViewModeIndex))]
    public partial ModelsViewMode ViewMode { get; set; } = ModelsViewMode.List;

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
