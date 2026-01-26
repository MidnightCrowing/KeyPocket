using System;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.UI.Controls;
using KeyPocket.UI.Messages;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI.Pages;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        ViewModel = new HomeViewModel(App.ProviderService);

        // Initial Visual State Check
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
        EmptyStatePanel.Visibility = ViewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnAddProviderClicked(object sender, RoutedEventArgs e)
    {
        // Create default provider directly
        var newProvider = App.ProviderService.CreateProvider();

        // Send creation message to trigger side bar update and navigation
        WeakReferenceMessenger.Default.Send(new ProviderCreatedMessage(newProvider.Id));
    }

    private void OnCopyKeyClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string fullKey)
        {
            // Copy to clipboard
            var dataPackage = new DataPackage();
            dataPackage.SetText(fullKey);
            Clipboard.SetContent(dataPackage);
        }
    }

    private void OnCopyModelIdClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement ele && ele.Tag is string modelId)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(modelId);
            Clipboard.SetContent(dataPackage);
        }
    }

    private void OnProviderTapped(object sender, TappedRoutedEventArgs e)
    {
        var senderElement = sender as FrameworkElement;

        // If button is clicked (e.g. copy button), ignore navigation
        if (e.OriginalSource is DependencyObject originalSource)
        {
            var element = originalSource as FrameworkElement;
            // Walk up the visual tree to check if source is within a Button
            while (element != null && element != senderElement)
            {
                if (element is Button || element is CopyButton) return;
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
        }

        if (senderElement?.DataContext is ProviderViewModel item) NavigateToProvider(item.Id);
    }

    private void NavigateToProvider(Guid providerId)
    {
        Frame.Navigate(typeof(ProviderSettingsPage), providerId.ToString());

        // Update sidebar selection
        var mainWindow = App.MainWindow as MainWindow;
        if (mainWindow != null) mainWindow.SelectProviderInSidebar(providerId);
    }

    private void OnCopyBaseUrlClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string baseUrl)
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(baseUrl);
                Clipboard.SetContent(dataPackage);
            }
            catch
            {
                // Silently fail
            }
    }
}