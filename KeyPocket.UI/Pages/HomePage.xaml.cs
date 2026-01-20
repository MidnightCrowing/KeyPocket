using System;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;
using KeyPocket.UI.Dialogs;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;
using KeyPocket.UI.ViewModels;

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
            
            // Register for provider deletion messages
            WeakReferenceMessenger.Default.Register<ProviderDeletedMessage>(this, (r, m) =>
            {
                ViewModel.Refresh();
            });



        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.Refresh();
        }

        private void UpdateVisualState()
        {
            EmptyStatePanel.Visibility = ViewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
            ContentGrid.Visibility = ViewModel.IsNotEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void OnAddProviderClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new AddProviderDialog();
            dialog.RequestedTheme = ThemeHelper.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                App.ProviderService.CreateProvider(
                    dialog.ProviderName,
                    dialog.ProviderType,
                    dialog.BaseUrl,
                    dialog.Description
                );

                ViewModel.Refresh();
            }
        }

        private void OnCopyKeyClicked(object sender, RoutedEventArgs e)
        {
            // Copy logic stub
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
            // Copy logic stub
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
                Frame.Navigate(typeof(ProviderSettingsPage), item.Id);
                
                // Update sidebar selection
                var mainWindow = App.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.SelectProviderInSidebar(item.Id);
                }
            }
        }
    }
}

