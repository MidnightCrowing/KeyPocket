using System;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.UI.Messages;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI.Pages;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        ViewModel = new HomeViewModel(App.ProviderService);

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HomeViewModel.IsEmpty) ||
                e.PropertyName == nameof(HomeViewModel.IsNotEmpty))
                UpdateVisualState();
        };
        UpdateVisualState();
    }

    public HomeViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Refresh();
    }

    private void UpdateVisualState()
    {
        var isEmpty = ViewModel.IsEmpty;
        EmptyStatePanel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        DisclaimerInfoBar.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnAddProviderClicked(object sender, RoutedEventArgs e)
    {
        var newProvider = App.ProviderService.CreateProvider();
        WeakReferenceMessenger.Default.Send(new ProviderCreatedMessage(newProvider.Id));
    }

    private void OnProviderNavigateRequested(object sender, Guid providerId)
    {
        NavigateToProvider(providerId);
    }

    private void NavigateToProvider(Guid providerId)
    {
        Frame.Navigate(typeof(ProviderSettingsPage), providerId.ToString());

        var mainWindow = App.MainWindow as MainWindow;
        if (mainWindow != null) mainWindow.SelectProviderInSidebar(providerId);
    }
}