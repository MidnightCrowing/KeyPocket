using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.Core.Services;
using KeyPocket.UI.Messages;

namespace KeyPocket.UI.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ProviderService? _providerService; // Nullable if design-time or deferred

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsEmpty))] [NotifyPropertyChangedFor(nameof(IsNotEmpty))]
    private ObservableCollection<ProviderViewModel> _providers = new();

    public HomeViewModel(ProviderService providerService)
    {
        _providerService = providerService;
        LoadProviders();

        // Subscribe to Theme Changed
        WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, OnThemeChanged);
    }

    // Default constructor for XAML designer (optional)
    public HomeViewModel()
    {
    }

    public bool IsEmpty => Providers.Count == 0;
    public bool IsNotEmpty => Providers.Count > 0;

    private void OnThemeChanged(object recipient, ThemeChangedMessage message)
    {
        if (recipient is HomeViewModel vm)
            // Update all provider icons
            foreach (var p in vm.Providers)
                p.RefreshIcon();
    }

    public void LoadProviders()
    {
        if (_providerService == null) return;

        var coreProviders = _providerService.GetAllProviders();
        Providers.Clear();
        foreach (var p in coreProviders) Providers.Add(new ProviderViewModel(p, _providerService));
    }

    public void Refresh()
    {
        LoadProviders();
    }
}