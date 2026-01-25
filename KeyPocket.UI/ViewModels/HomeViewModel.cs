using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.Core.Services;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.UI.Messages;

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
        
        // Subscribe to Theme Changed
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, OnThemeChanged);
    }

    private void OnThemeChanged(object recipient, Messages.ThemeChangedMessage message)
    {
        if (recipient is HomeViewModel vm)
        {
             // Update all provider icons
             foreach (var p in vm.Providers)
             {
                 p.RefreshIcon();
             }
        }
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
