using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.Core.Services;

namespace KeyPocket.UI.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ProviderService? _providerService; // Nullable if design-time or deferred

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(IsNotEmpty))]
    private ObservableCollection<ProviderViewModel> _providers = new();

    public bool IsEmpty => Providers.Count == 0;
    public bool IsNotEmpty => Providers.Count > 0;

    public HomeViewModel(ProviderService providerService)
    {
        _providerService = providerService;
        LoadProviders();
    }

    // Default constructor for XAML designer (optional)
    public HomeViewModel() 
    {
    }

    public void LoadProviders()
    {
        if (_providerService == null) return;

        var coreProviders = _providerService.GetAllProviders();
        Providers.Clear();
        foreach (var p in coreProviders)
        {
            Providers.Add(new ProviderViewModel(p, _providerService));
        }
    }

    public void Refresh()
    {
        LoadProviders();
    }
}
