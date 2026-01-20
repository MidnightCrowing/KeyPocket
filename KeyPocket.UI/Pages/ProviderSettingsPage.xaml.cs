using KeyPocket.Core.Models;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using KeyPocket.UI.Helpers;
using System;
using System.Linq;

namespace KeyPocket.UI.Pages;

public sealed partial class ProviderSettingsPage : Page
{
    public ProviderSettingsViewModel ViewModel { get; private set; } = null!;

    public ProviderSettingsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string providerIdStr && System.Guid.TryParse(providerIdStr, out var providerId))
        {
            var provider = App.ProviderService.GetAllProviders().FirstOrDefault(p => p.Id == providerId);
            if (provider != null)
            {
                ViewModel = new ProviderSettingsViewModel(provider, App.ProviderService);
            }
        }
    }

    private void OnKeyTagKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && sender is TextBox tb && tb.DataContext is KeyWrapper wrapper)
        {
            wrapper.CommitTagEditCommand?.Execute(null);
            e.Handled = true;
        }
    }

    private void OnCopyKeyClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid keyId)
        {
            try
            {
                var plainKey = ViewModel.GetDecryptedKey(keyId);
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(plainKey);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            }
            catch
            {
                // Silently fail
            }
        }
    }

    private async void OnDeleteProviderClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Delete Provider",
            Content = "Are you sure you want to delete this provider? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };
        
        dialog.RequestedTheme = ThemeHelper.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.DeleteProvider();
            
            // Navigate back to home
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                Frame.Navigate(typeof(HomePage));
            }
        }
    }

    private void OnCopyModelIdClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string modelId)
        {
            try
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(modelId);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            }
            catch
            {
                // Silently fail
            }
        }
    }

    private void OnModelEditKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && sender is FrameworkElement element && element.DataContext is ModelWrapper wrapper)
        {
            wrapper.CancelAddCommand?.Execute(null);
            e.Handled = true;
        }
    }

    private void OnApiKeyEditKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && sender is FrameworkElement element && element.DataContext is KeyWrapper wrapper)
        {
            wrapper.CancelAddCommand?.Execute(null);
            e.Handled = true;
        }
    }
    private async void OnChangeIconClicked(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        
        // WinUI 3 Window handle workaround
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".ico");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        if (file != null)
        {
            await ViewModel.UpdateIconAsync(file);
        }
    }

    private async void OnRemoveIconClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateIconAsync(null);
    }
}
