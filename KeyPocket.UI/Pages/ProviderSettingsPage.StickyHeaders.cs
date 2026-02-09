using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace KeyPocket.UI.Pages;

public sealed partial class ProviderSettingsPage
{
    private const double StickyHeaderHeight = 60;
    private double _apiKeysTop;
    private double _generalTop;
    private double _modelsTop;
    private Border? _stickyApiKeys;
    private InfoBadge? _stickyApiKeysBadge;
    private Border? _stickyGeneral;
    private Border? _stickyModels;
    private InfoBadge? _stickyModelsBadge;

    private void CreateStickyHeaders()
    {
        var resourceLoader = ResourceLoader.GetForViewIndependentUse();

        _stickyGeneral = CreateStickyHeaderBorder(
            resourceLoader.GetString("ProviderSettings_General/Text"),
            resourceLoader.GetString("ProviderSettings_GeneralDesc/Text"));
        StickyHeadersCanvas.Children.Add(_stickyGeneral);

        (_stickyApiKeys, _stickyApiKeysBadge) = CreateStickyHeaderBorderWithBadge(
            resourceLoader.GetString("ProviderSettings_ApiKeys/Text"),
            resourceLoader.GetString("ProviderSettings_ApiKeysDesc/Text"),
            ViewModel?.ApiKeys.Count ?? 0);
        StickyHeadersCanvas.Children.Add(_stickyApiKeys);

        (_stickyModels, _stickyModelsBadge) = CreateStickyHeaderBorderWithBadge(
            resourceLoader.GetString("ProviderSettings_Models/Text"),
            resourceLoader.GetString("ProviderSettings_ModelsDesc/Text"),
            ViewModel?.Models.Count ?? 0);
        StickyHeadersCanvas.Children.Add(_stickyModels);

        _stickyGeneral.Visibility = Visibility.Collapsed;
        _stickyApiKeys.Visibility = Visibility.Collapsed;
        _stickyModels.Visibility = Visibility.Collapsed;

        if (ViewModel != null)
        {
            ViewModel.ApiKeys.CollectionChanged += (s, e) => UpdateStickyBadgeCounts();
            ViewModel.Models.CollectionChanged += (s, e) => UpdateStickyBadgeCounts();
        }
    }

    private Border CreateStickyHeaderBorder(string title, string subtitle)
    {
        var currentTheme = ThemeHelper.Theme == ElementTheme.Default
            ? Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light
            : ThemeHelper.Theme;

        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 12, 0, 12),
            Width = 270,
            Height = 70,
            RequestedTheme = currentTheme
        };

        if (currentTheme == ElementTheme.Dark)
            border.Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));
        else
            border.Background = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));

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

        if (currentTheme == ElementTheme.Dark)
        {
            titleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            subtitleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 161, 161, 161));
        }
        else
        {
            titleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            subtitleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 96, 96, 96));
        }

        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(subtitleBlock);
        border.Child = stackPanel;

        Canvas.SetLeft(border, 40);
        Canvas.SetTop(border, 0);
        Canvas.SetZIndex(border, 100);

        return border;
    }

    private (Border, InfoBadge) CreateStickyHeaderBorderWithBadge(string title, string subtitle, int count)
    {
        var currentTheme = ThemeHelper.Theme == ElementTheme.Default
            ? Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light
            : ThemeHelper.Theme;

        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 12, 0, 12),
            Width = 270,
            Height = 70,
            RequestedTheme = currentTheme
        };

        if (currentTheme == ElementTheme.Dark)
            border.Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));
        else
            border.Background = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));

        var stackPanel = new StackPanel { Spacing = 4 };

        var titleRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var titleBlock = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"]
        };

        var infoBadge = new InfoBadge
        {
            Value = count,
            VerticalAlignment = VerticalAlignment.Center
        };

        titleRow.Children.Add(titleBlock);
        titleRow.Children.Add(infoBadge);

        var subtitleBlock = new TextBlock
        {
            Text = subtitle,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"]
        };

        if (currentTheme == ElementTheme.Dark)
        {
            titleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            subtitleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 161, 161, 161));
        }
        else
        {
            titleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            subtitleBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 96, 96, 96));
        }

        stackPanel.Children.Add(titleRow);
        stackPanel.Children.Add(subtitleBlock);
        border.Child = stackPanel;

        Canvas.SetLeft(border, 40);
        Canvas.SetTop(border, 0);
        Canvas.SetZIndex(border, 100);

        return (border, infoBadge);
    }

    private void UpdateStickyBadgeCounts()
    {
        if (_stickyApiKeysBadge != null && ViewModel != null)
            _stickyApiKeysBadge.Value = ViewModel.ApiKeys.Count;

        if (_stickyModelsBadge != null && ViewModel != null)
            _stickyModelsBadge.Value = ViewModel.Models.Count;
    }

    private void CalculateSectionPositions()
    {
        try
        {
            var scrollContent = MainScrollViewer.Content as FrameworkElement;
            if (scrollContent == null) return;

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
            // Ignore errors during position calculation
        }
    }

    private void OnScrollViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_stickyGeneral == null || _stickyApiKeys == null || _stickyModels == null)
            return;

        var scrollOffset = MainScrollViewer.VerticalOffset;

        if (scrollOffset >= _modelsTop - StickyHeaderHeight)
        {
            _stickyGeneral.Visibility = Visibility.Collapsed;
            _stickyApiKeys.Visibility = Visibility.Collapsed;
            _stickyModels.Visibility = Visibility.Visible;
        }
        else if (scrollOffset >= _apiKeysTop - StickyHeaderHeight)
        {
            _stickyGeneral.Visibility = Visibility.Collapsed;
            _stickyApiKeys.Visibility = Visibility.Visible;
            _stickyModels.Visibility = Visibility.Collapsed;
        }
        else if (scrollOffset >= _generalTop - StickyHeaderHeight)
        {
            _stickyGeneral.Visibility = Visibility.Visible;
            _stickyApiKeys.Visibility = Visibility.Collapsed;
            _stickyModels.Visibility = Visibility.Collapsed;
        }
        else
        {
            _stickyGeneral.Visibility = Visibility.Collapsed;
            _stickyApiKeys.Visibility = Visibility.Collapsed;
            _stickyModels.Visibility = Visibility.Collapsed;
        }
    }
}