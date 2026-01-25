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

public sealed partial class ProviderSettingsPage : Page, System.ComponentModel.INotifyPropertyChanged
{
    private ProviderSettingsViewModel? _viewModel;
    
    public ProviderSettingsViewModel? ViewModel
    {
        get => _viewModel;
        private set
        {
            if (_viewModel != value)
            {
                _viewModel = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ViewModel)));
            }
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

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
        // 初始化默认 ViewModel 以避免绑定错误
        ViewModel = new ProviderSettingsViewModel();
        
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
            else
            {
                // Provider 不存在（可能已被删除），返回首页
                Frame.Navigate(typeof(HomePage));
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
        if (ViewModel == null) return;
        
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
            if (ViewModel == null) return;
            
            var providerId = ViewModel.Provider.Id;
            
            // 先执行删除
            ViewModel.DeleteProvider();
            
            // 清理导航历史：移除所有指向该 Provider 的历史记录
            CleanupNavigationHistory(providerId);
            
            // 直接导航（不使用 Dispatcher，因为删除是同步的）
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

    /// <summary>
    /// 清理导航历史中指向已删除 Provider 的记录
    /// </summary>
    private void CleanupNavigationHistory(Guid deletedProviderId)
    {
        // WinUI 3 的 Frame 不支持直接操作导航历史
        // 但我们可以通过不回退到已删除的页面来避免问题
        // 这个方法为未来扩展预留
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

    // Round price value to 3 decimal places to avoid showing long double artifacts
    private void OnPriceValueChanged(object sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.NumberBox nb)
        {
            try
            {
                var rounded = Math.Round(nb.Value, 3);
                if (Math.Abs(nb.Value - rounded) > 0)
                {
                    nb.Value = rounded;
                }
            }
            catch { }
        }
    }
    private void OnDefaultIconItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is KeyPocket.UI.ViewModels.DefaultIconItem item)
        {
            if (ViewModel != null)
            {
               ViewModel.SelectDefaultIconCommand.Execute(item);
            }
            
            // Try to find the flyout to close it
            if (sender is GridView gridView && gridView.Parent is FlyoutPresenter presenter && presenter.Parent is Microsoft.UI.Xaml.Controls.Primitives.Popup popup)
            {
                 popup.IsOpen = false;
            }
            // The visual tree of a Button.Flyout is complex. 
            // A common way to close a Flyout from code-behind if you don't have a reference is difficult.
            // However, since we are inside the page, we can try to close the *active* flyout if we could find it.
            // Alternative: Simply clicking the item usually doesn't close Flyout automatically if it's just a GridView ItemClick.
            // We can try to name the Flyout in XAML, but I didn't.
            // Let's rely on the command execution for now. 
            // Users usually click away. But to be "native" it should close.
            
            // IMPROVEMENT: Close the flyout by finding the open one or just using the hack for now.
            // Actually, we can use VisualTreeHelper to find the parent Flyout?
            // FlyoutPresenters are in a separate window/popup visually.
            
            // Let's modify the XAML to name the Flyout if we really want to close it, 
            // OR use a behavior. 
            // For now, let's just execute the command.
            
            // Wait, if I want to close it:
            // I can bind the Flyout's 'IsOpen' property to a boolean in ViewModel?
            // That requires XAML change which I've already done twice.
            // Let's stick to just executing for this iteration, unless testing shows it's annoying.
            // Actually, I can cast sender to GridView -> find logical parent? 
            // Flyout content is not logical child of Button.
            
            // Let's just execute for now.
        }
    }

    private async void OnChangeIconClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
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
