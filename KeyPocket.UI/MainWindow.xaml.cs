using System;
using System.Collections.Generic;
using KeyPocket.UI.Pages;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI;

public sealed partial class MainWindow
{
    private readonly Dictionary<Type, NavigationViewItem> _pageToNavItem;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);

        _pageToNavItem = new Dictionary<Type, NavigationViewItem>
        {
            { typeof(HomePage), homePage },
            { typeof(SamplePage2), samplePage2Item },
            { typeof(SamplePage3), samplePage3Item }
        };

        navView.SelectedItem = homePage;
        contentFrame.Navigate(typeof(HomePage));

        contentFrame.Navigated += OnContentFrameNavigated;
    }

    private void TitleBar_OnBackRequested(TitleBar sender, object args)
    {
        if (contentFrame.CanGoBack) contentFrame.GoBack();
    }

    private void TitleBar_OnPaneToggleRequested(TitleBar sender, object args)
    {
        navView.IsPaneOpen = !navView.IsPaneOpen;
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            sender.Header = "Settings";
            NavigateTo(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            sender.Header = item.Content?.ToString();

            var pageType = tag switch
            {
                "HomePage" => typeof(HomePage),
                "SamplePage2" => typeof(SamplePage2),
                "SamplePage3" => typeof(SamplePage3),
                _ => null
            };
            if (pageType != null)
                NavigateTo(pageType);
        }
    }

    private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
    {
        var canGoBack = contentFrame?.CanGoBack ?? false;
        titleBar.IsBackButtonVisible = canGoBack;

        if (contentFrame?.Content != null)
            NavigateTo(contentFrame.Content.GetType());
    }

    private void NavigateTo(Type pageType)
    {
        if (contentFrame.Content?.GetType() != pageType) // 避免重复导航
            contentFrame.Navigate(pageType);

        // 同步 NavView 选中状态
        if (_pageToNavItem.TryGetValue(pageType, out var navItem))
            navView.SelectedItem = navItem;
    }
}