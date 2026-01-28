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
    // ========== 手动拖拽事件处理 ==========

    private ProviderViewModel? _draggingProvider;

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

    private void OnCardDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is FrameworkElement element && element.DataContext is ProviderViewModel provider)
        {
            _draggingProvider = provider;
            args.Data.RequestedOperation = DataPackageOperation.Move;
            // 设置拖拽数据
            args.Data.Properties.Add("ProviderId", provider.Id.ToString());
        }
    }

    private void OnCardDragOver(object sender, DragEventArgs e)
    {
        // 允许放置
        e.AcceptedOperation = DataPackageOperation.Move;
        e.DragUIOverride.Caption = "Move";
        e.DragUIOverride.IsGlyphVisible = true;
    }

    private void OnCardDrop(object sender, DragEventArgs e)
    {
        if (_draggingProvider == null) return;

        // 获取目标位置的 Provider
        if (sender is FrameworkElement element && element.DataContext is ProviderViewModel targetProvider)
        {
            if (_draggingProvider.Id == targetProvider.Id) return; // 同一个项，忽略

            // 获取当前索引
            var oldIndex = ViewModel.Providers.IndexOf(_draggingProvider);
            var newIndex = ViewModel.Providers.IndexOf(targetProvider);

            if (oldIndex == -1 || newIndex == -1) return;

            // 移动项
            ViewModel.Providers.Move(oldIndex, newIndex);

            // 保存新顺序
            ViewModel.UpdateProviderOrder();
        }

        _draggingProvider = null;
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

    private void OnCopyModelIdClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement ele && ele.Tag is string modelId)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(modelId);
            Clipboard.SetContent(dataPackage);
        }
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