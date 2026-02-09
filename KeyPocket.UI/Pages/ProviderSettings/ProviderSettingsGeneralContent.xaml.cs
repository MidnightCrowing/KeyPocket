using System;
using Windows.Storage.Pickers;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace KeyPocket.UI.Pages.ProviderSettings;

public sealed partial class ProviderSettingsGeneralContent : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ProviderSettingsViewModel),
            typeof(ProviderSettingsGeneralContent),
            new PropertyMetadata(null));

    public ProviderSettingsGeneralContent()
    {
        InitializeComponent();
    }

    public ProviderSettingsViewModel? ViewModel
    {
        get => (ProviderSettingsViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private async void OnDefaultIconItemClick(object sender, ItemClickEventArgs e)
    {
        if (ViewModel == null) return;

        if (e.ClickedItem is DefaultIconItem item)
        {
            await ViewModel.SelectDefaultIcon(item);
            DefaultIconFlyout.Hide();
        }
    }

    private async void OnChangeIconClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".ico");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".gif");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
            await ViewModel.UpdateIconAsync(file);
    }

    private async void OnRemoveIconClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        await ViewModel.UpdateIconAsync(null);
    }
}