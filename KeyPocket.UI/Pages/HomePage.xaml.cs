using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;
using KeyPocket.UI.ViewModels;
using KeyPocket.UI.Controls;

namespace KeyPocket.UI.Pages
{
    public sealed partial class HomePage : Page
    {
        public HomeViewModel ViewModel { get; private set; }

        public HomePage()
        {
            this.InitializeComponent();
            ViewModel = new HomeViewModel(App.ProviderService);

            // Initial Visual State Check
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HomeViewModel.IsEmpty) ||
                    e.PropertyName == nameof(HomeViewModel.IsNotEmpty))
                {
                    UpdateVisualState();
                }
            };
            UpdateVisualState();
            




        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
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
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(fullKey);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            }
        }

        private void OnCopyModelIdClicked(object sender, RoutedEventArgs e)
        {

            if (sender is FrameworkElement ele && ele.Tag is string modelId)
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(modelId);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            }
        }


        private void OnProviderItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ViewModels.ProviderViewModel item)
            {
                NavigateToProvider(item.Id);
            }
        }

        private void OnProviderTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var senderElement = sender as FrameworkElement;
            
            // If button is clicked (e.g. copy button), ignore navigation
            if (e.OriginalSource is DependencyObject originalSource)
            {
                var element = originalSource as FrameworkElement;
                // Walk up the visual tree to check if source is within a Button
                while (element != null && element != senderElement)
                {
                    if (element is Button || element is KeyPocket.UI.Controls.CopyButton)
                    {
                        return;
                    }
                    element = VisualTreeHelper.GetParent(element) as FrameworkElement;
                }
            }

            if (senderElement?.DataContext is ViewModels.ProviderViewModel item)
            {
                NavigateToProvider(item.Id);
            }
        }

        private void NavigateToProvider(Guid providerId)
        {
            Frame.Navigate(typeof(ProviderSettingsPage), providerId.ToString());
            
            // Update sidebar selection
            var mainWindow = App.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.SelectProviderInSidebar(providerId);
            }
        }

        private void OnCopyBaseUrlClicked(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string baseUrl)
            {
                try
                {
                    var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dataPackage.SetText(baseUrl);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                }
                catch
                {
                    // Silently fail
                }
            }
        }
    }
}

