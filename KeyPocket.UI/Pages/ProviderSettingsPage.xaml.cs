using System;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

namespace KeyPocket.UI.Pages;

public sealed partial class ProviderSettingsPage : Page, INotifyPropertyChanged
{
    private const double StickyHeaderHeight = 60;
    private double _apiKeysTop;
    private double _generalTop;
    private double _modelsTop;
    private Border? _stickyApiKeys;

    // Sticky Headers fields
    private Border? _stickyGeneral;
    private Border? _stickyModels;
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

    private void OnKeyTagKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && sender is TextBox tb && tb.DataContext is KeyWrapper wrapper)
        {
            wrapper.CommitTagEditCommand?.Execute(null);
            e.Handled = true;
        }
    }

    private void OnCopyKeyClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        if (sender is Button btn && btn.Tag is Guid keyId)
            try
            {
                var plainKey = ViewModel.GetDecryptedKey(keyId);
                var dataPackage = new DataPackage();
                dataPackage.SetText(plainKey);
                Clipboard.SetContent(dataPackage);
            }
            catch
            {
                // Silently fail
            }
    }

    private async void OnDeleteProviderClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Delete Provider",
            Content = "Are you sure you want to delete this provider? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
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

    private void OnCopyModelIdClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string modelId)
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(modelId);
                Clipboard.SetContent(dataPackage);
            }
            catch
            {
                // Silently fail
            }
    }

    private void OnModelEditKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && sender is FrameworkElement element &&
            element.DataContext is ModelWrapper wrapper)
        {
            wrapper.CancelAddCommand?.Execute(null);
            e.Handled = true;
        }
    }

    private void OnApiKeyEditKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && sender is FrameworkElement element &&
            element.DataContext is KeyWrapper wrapper)
        {
            wrapper.CancelAddCommand?.Execute(null);
            e.Handled = true;
        }
    }

    // Round price value to 3 decimal places to avoid showing long double artifacts
    private void OnPriceValueChanged(object sender, NumberBoxValueChangedEventArgs e)
    {
        if (sender is NumberBox nb)
            try
            {
                var rounded = Math.Round(nb.Value, 3);
                if (Math.Abs(nb.Value - rounded) > 0) nb.Value = rounded;
            }
            catch
            {
            }
    }

    private void OnDefaultIconItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DefaultIconItem item)
        {
            if (ViewModel != null) ViewModel.SelectDefaultIconCommand.Execute(item);

            // Try to find the flyout to close it
            if (sender is GridView gridView && gridView.Parent is FlyoutPresenter presenter &&
                presenter.Parent is Popup popup) popup.IsOpen = false;
            // Try to close flight if possible, or rely on native behavior
            // Current code executes the command directly.
        }
    }

    private async void OnChangeIconClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var picker = new FileOpenPicker();

        // WinUI 3 Window handle workaround
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".ico");

        var file = await picker.PickSingleFileAsync();
        if (file != null) await ViewModel.UpdateIconAsync(file);
    }

    private async void OnRemoveIconClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        await ViewModel.UpdateIconAsync(null);
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        CreateStickyHeaders();
        CalculateSectionPositions();
    }

    private void CreateStickyHeaders()
    {
        // Create sticky header for General
        _stickyGeneral = CreateStickyHeaderBorder("General", "Basic provider information.");
        StickyHeadersCanvas.Children.Add(_stickyGeneral);

        // Create sticky header for API Keys
        _stickyApiKeys = CreateStickyHeaderBorder("API Keys", "Manage access keys for this provider.");
        StickyHeadersCanvas.Children.Add(_stickyApiKeys);

        // Create sticky header for Models
        _stickyModels = CreateStickyHeaderBorder("Models", "Configure available models.");
        StickyHeadersCanvas.Children.Add(_stickyModels);

        // Initially hide all sticky headers
        _stickyGeneral.Visibility = Visibility.Collapsed;
        _stickyApiKeys.Visibility = Visibility.Collapsed;
        _stickyModels.Visibility = Visibility.Collapsed;
    }

    private Border CreateStickyHeaderBorder(string title, string subtitle)
    {
        // Determine the current theme
        var currentTheme = ThemeHelper.Theme == ElementTheme.Default
            ? Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light
            : ThemeHelper.Theme;

        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 12, 0, 12),
            Width = 270, // Match the left column width
            Height = 70, // Increased height to prevent text clipping
            RequestedTheme = currentTheme
        };

        // Manually set colors based on theme
        // These colors match WinUI 3's default theme colors
        if (currentTheme == ElementTheme.Dark)
            // Dark theme colors
            border.Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32)); // #202020
        else
            // Light theme colors
            border.Background = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243)); // #F3F3F3

        var stackPanel = new StackPanel { Spacing = 4 };

        var titleBlock = new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"]
        };

        var subtitleBlock = new TextBlock
        {
            Text = subtitle,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"]
        };

        // Manually set text colors based on theme
        if (currentTheme == ElementTheme.Dark)
        {
            // Dark theme text colors
            titleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // White for primary text
            subtitleBlock.Foreground =
                new SolidColorBrush(Color.FromArgb(255, 161, 161, 161)); // #A1A1A1 for secondary text
        }
        else
        {
            // Light theme text colors
            titleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)); // Black for primary text
            subtitleBlock.Foreground =
                new SolidColorBrush(Color.FromArgb(255, 96, 96, 96)); // #606060 for secondary text
        }

        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(subtitleBlock);
        border.Child = stackPanel;

        Canvas.SetLeft(border, 40); // Match the page padding
        Canvas.SetTop(border, 0);
        Canvas.SetZIndex(border, 100);

        return border;
    }

    private void CalculateSectionPositions()
    {
        try
        {
            var scrollContent = MainScrollViewer.Content as FrameworkElement;
            if (scrollContent == null) return;

            // Get positions relative to scroll content
            if (GeneralHeader != null)
            {
                var transform = GeneralHeader.TransformToVisual(scrollContent);
                var point = transform.TransformPoint(new Point(0, 0));
                _generalTop = point.Y;
            }

            if (ApiKeysHeader != null)
            {
                var transform = ApiKeysHeader.TransformToVisual(scrollContent);
                var point = transform.TransformPoint(new Point(0, 0));
                _apiKeysTop = point.Y;
            }

            if (ModelsHeader != null)
            {
                var transform = ModelsHeader.TransformToVisual(scrollContent);
                var point = transform.TransformPoint(new Point(0, 0));
                _modelsTop = point.Y;
            }
        }
        catch
        {
            /* Ignore errors during position calculation */
        }
    }

    private void OnScrollViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_stickyGeneral == null || _stickyApiKeys == null || _stickyModels == null)
            return;

        var scrollOffset = MainScrollViewer.VerticalOffset;

        // Determine which header should be sticky
        if (scrollOffset >= _modelsTop - StickyHeaderHeight)
        {
            // Models section is at top
            _stickyGeneral.Visibility = Visibility.Collapsed;
            _stickyApiKeys.Visibility = Visibility.Collapsed;
            _stickyModels.Visibility = Visibility.Visible;
        }
        else if (scrollOffset >= _apiKeysTop - StickyHeaderHeight)
        {
            // API Keys section is at top
            _stickyGeneral.Visibility = Visibility.Collapsed;
            _stickyApiKeys.Visibility = Visibility.Visible;
            _stickyModels.Visibility = Visibility.Collapsed;
        }
        else if (scrollOffset >= _generalTop - StickyHeaderHeight)
        {
            // General section is at top
            _stickyGeneral.Visibility = Visibility.Visible;
            _stickyApiKeys.Visibility = Visibility.Collapsed;
            _stickyModels.Visibility = Visibility.Collapsed;
        }
        else
        {
            // No sticky header needed
            _stickyGeneral.Visibility = Visibility.Collapsed;
            _stickyApiKeys.Visibility = Visibility.Collapsed;
            _stickyModels.Visibility = Visibility.Collapsed;
        }
    }
}