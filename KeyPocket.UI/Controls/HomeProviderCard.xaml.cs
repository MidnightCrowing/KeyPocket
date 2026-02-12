using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace KeyPocket.UI.Controls;

public sealed partial class HomeProviderCard : UserControl
{
    public static readonly DependencyProperty ProviderProperty =
        DependencyProperty.Register(
            nameof(Provider),
            typeof(ProviderViewModel),
            typeof(HomeProviderCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HomeViewModelProperty =
        DependencyProperty.Register(
            nameof(HomeViewModel),
            typeof(HomeViewModel),
            typeof(HomeProviderCard),
            new PropertyMetadata(null));

    public HomeProviderCard()
    {
        InitializeComponent();
    }

    public ProviderViewModel? Provider
    {
        get => (ProviderViewModel?)GetValue(ProviderProperty);
        set => SetValue(ProviderProperty, value);
    }

    public HomeViewModel? HomeViewModel
    {
        get => (HomeViewModel?)GetValue(HomeViewModelProperty);
        set => SetValue(HomeViewModelProperty, value);
    }

    public event EventHandler<Guid>? NavigateRequested;

    private void OnCopyKeyClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string fullKey)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(fullKey);
            Clipboard.SetContent(dataPackage);
        }
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

    private void OnCardDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (Provider == null) return;

        args.Data.RequestedOperation = DataPackageOperation.Move;
        args.Data.Properties.Add("ProviderId", Provider.Id.ToString());
    }

    private void OnCardDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
        e.DragUIOverride.Caption = "Move";
        e.DragUIOverride.IsGlyphVisible = true;
    }

    private void OnCardDrop(object sender, DragEventArgs e)
    {
        if (HomeViewModel == null || Provider == null) return;

        var properties = e.DataView?.Properties;
        if (properties == null) return;
        if (!properties.TryGetValue("ProviderId", out var value)) return;
        if (value is not string providerIdStr || !Guid.TryParse(providerIdStr, out var providerId)) return;
        if (providerId == Provider.Id) return;

        var draggingProvider = HomeViewModel.Providers.FirstOrDefault(p => p.Id == providerId);
        if (draggingProvider == null) return;

        var oldIndex = HomeViewModel.Providers.IndexOf(draggingProvider);
        var newIndex = HomeViewModel.Providers.IndexOf(Provider);
        if (oldIndex == -1 || newIndex == -1) return;

        HomeViewModel.Providers.Move(oldIndex, newIndex);
        HomeViewModel.UpdateProviderOrder();
    }

    private void OnProviderTapped(object sender, TappedRoutedEventArgs e)
    {
        if (Provider == null) return;

        var senderElement = sender as FrameworkElement;

        if (e.OriginalSource is DependencyObject originalSource)
        {
            var element = originalSource as FrameworkElement;
            while (element != null && element != senderElement)
            {
                if (element is Button || element is CopyButton) return;
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
        }

        NavigateRequested?.Invoke(this, Provider.Id);
    }
}
