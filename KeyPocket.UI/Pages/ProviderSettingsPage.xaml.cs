using KeyPocket.Core.Models;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using KeyPocket.UI.Helpers;
using System;
using System.Linq;
using Microsoft.UI.Xaml.Media;

namespace KeyPocket.UI.Pages;

public sealed partial class ProviderSettingsPage : Page
{
    public ProviderSettingsViewModel ViewModel { get; private set; } = null!;

    // Sticky Headers fields
    private Border? _stickyGeneral;
    private Border? _stickyApiKeys;
    private Border? _stickyModels;
    private double _generalTop;
    private double _apiKeysTop;
    private double _modelsTop;
    private const double StickyHeaderHeight = 60;

    public ProviderSettingsPage()
    {
        this.InitializeComponent();
        this.Loaded += OnPageLoaded;
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
            // 先执行删除
            ViewModel.DeleteProvider();
            
            // 使用 Dispatcher 确保在 UI 线程上执行导航，并稍微延迟以确保删除完成
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                // Navigate back to home
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                else
                {
                    Frame.Navigate(typeof(HomePage));
                }
            });
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
        {
            await ViewModel.UpdateIconAsync(file);
        }
    }

    private async void OnRemoveIconClicked(object sender, RoutedEventArgs e)
    {
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
            ? (Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light)
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
        {
            // Dark theme colors
            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 32, 32, 32)); // #202020
        }
        else
        {
            // Light theme colors
            border.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 243, 243, 243)); // #F3F3F3
        }

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
            titleBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)); // White for primary text
            subtitleBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 161, 161, 161)); // #A1A1A1 for secondary text
        }
        else
        {
            // Light theme text colors
            titleBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)); // Black for primary text
            subtitleBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 96, 96, 96)); // #606060 for secondary text
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
                var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                _generalTop = point.Y;
            }

            if (ApiKeysHeader != null)
            {
                var transform = ApiKeysHeader.TransformToVisual(scrollContent);
                var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                _apiKeysTop = point.Y;
            }

            if (ModelsHeader != null)
            {
                var transform = ModelsHeader.TransformToVisual(scrollContent);
                var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                _modelsTop = point.Y;
            }
        }
        catch { /* Ignore errors during position calculation */ }
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
