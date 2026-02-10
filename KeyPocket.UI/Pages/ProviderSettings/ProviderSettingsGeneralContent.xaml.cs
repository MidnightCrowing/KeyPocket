using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace KeyPocket.UI.Pages.ProviderSettings;

public sealed partial class ProviderSettingsGeneralContent : ProviderSettingsSectionBase
{
    public ProviderSettingsGeneralContent()
    {
        InitializeComponent();
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

    public override Task SaveAsync()
    {
        ViewModel?.SaveGeneral();
        return Task.CompletedTask;
    }
}
