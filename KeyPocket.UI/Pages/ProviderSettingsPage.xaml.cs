using System;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.Resources;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI.Pages;

public sealed partial class ProviderSettingsPage : Page, INotifyPropertyChanged
{
    private ProviderSettingsViewModel? _viewModel;

    public ProviderSettingsPage()
    {
        // Initialize default ViewModel to avoid binding errors
        ViewModel = new ProviderSettingsViewModel();

        InitializeComponent();
        Loaded += OnPageLoaded;

    }

    public ProviderSettingsViewModel? ViewModel
    {
        get => _viewModel;
        private set
        {
            if (_viewModel != value)
            {
                _viewModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string providerIdStr && Guid.TryParse(providerIdStr, out var providerId))
        {
            var provider = App.ProviderService.GetAllProviders().FirstOrDefault(p => p.Id == providerId);
            if (provider != null)
                ViewModel = new ProviderSettingsViewModel(provider, App.ProviderService);
            else
                // Provider does not exist (may have been deleted), return to home
                Frame.Navigate(typeof(HomePage));
        }
    }

    private async void OnDeleteProviderClicked(object sender, RoutedEventArgs e)
    {
        var resourceLoader = ResourceLoader.GetForViewIndependentUse();
        var dialog = new ContentDialog
        {
            Title = resourceLoader.GetString("ProviderSettings_DeleteDialog_Title"),
            Content = resourceLoader.GetString("ProviderSettings_DeleteDialog_Content"),
            PrimaryButtonText = resourceLoader.GetString("ProviderSettings_DeleteDialog_PrimaryButton"),
            CloseButtonText = resourceLoader.GetString("ProviderSettings_DeleteDialog_CloseButton"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        dialog.RequestedTheme = ThemeHelper.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (ViewModel == null) return;

            var providerId = ViewModel.Provider.Id;

            // 先执行删除
            ViewModel.DeleteProvider();

            // 清理导航历史：移除所有指向该 Provider 的历史记录
            CleanupNavigationHistory(providerId);

            // 直接导航（不使用 Dispatcher，因为删除是同步的）
            if (Frame.CanGoBack)
                Frame.GoBack();
            else
                Frame.Navigate(typeof(HomePage));
        }
    }

    /// <summary>
    ///     Cleanup navigation history for deleted provider
    /// </summary>
    private void CleanupNavigationHistory(Guid deletedProviderId)
    {
        // WinUI 3 Frame does not support direct manipulation of navigation history
        // But we can avoid issues by not navigating back to the deleted page
        // This method is reserved for future extensions
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        CreateStickyHeaders();
        CalculateSectionPositions();
    }
}
