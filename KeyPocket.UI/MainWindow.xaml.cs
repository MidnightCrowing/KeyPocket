using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.ApplicationModel.Resources;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;
using KeyPocket.UI.Models;
using KeyPocket.UI.Pages;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI;

public sealed partial class MainWindow
{
    private readonly Dictionary<Type, NavigationViewItem> _pageToNavItem;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            Title = resourceLoader.GetString("MainWindow.Title");
        }
        catch
        {
            /* Fallback to default if resource missing */
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);

        // 初始化 ViewModel
        ViewModel = new MainWindowViewModel(App.ProviderService);
        SearchViewModel = new SearchViewModel(App.ProviderService);

        _pageToNavItem = new Dictionary<Type, NavigationViewItem>
        {
            { typeof(HomePage), HomePage },
            { typeof(ModelsPage), ModelsPageItem },
            { typeof(KeysPage), KeysPageItem }
        };

        NavView.SelectedItem = HomePage;
        ContentFrame.Navigate(typeof(HomePage));

        ContentFrame.Navigated += OnContentFrameNavigated;

        // Initialize Sidebar Providers
        LoadProvidersToSidebar();

        // Listen to Providers collection changes
        ViewModel.Providers.CollectionChanged += Providers_CollectionChanged;

        // Subscribe to Provider creation
        WeakReferenceMessenger.Default.Register<ProviderCreatedMessage>(this, (r, m) =>
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                ContentFrame.Navigate(typeof(ProviderSettingsPage), m.ProviderId.ToString());
                SelectProviderInSidebar(m.ProviderId);
            });
        });

        // Listen for Theme changes to update icons
        if (Content is FrameworkElement root) root.ActualThemeChanged += (s, e) => UpdateAllSidebarIcons();
    }

    public MainWindowViewModel ViewModel { get; }
    public SearchViewModel SearchViewModel { get; }

    private void UpdateAllSidebarIcons()
    {
        // 1. Update Sidebar items
        // Recursively or iterate menu items
        foreach (var item in NavView.MenuItems)
            if (item is NavigationViewItem navItem && navItem.Tag is string tag && tag.StartsWith("Provider_"))
                try
                {
                    var providerIdStr = tag.Substring("Provider_".Length);
                    if (Guid.TryParse(providerIdStr, out var pid))
                    {
                        var provider = ViewModel.Providers.FirstOrDefault(p => p.Id == pid);
                        if (provider != null)
                            navItem.Icon = ResolveIconElement(provider.IconPath, provider.DefaultIconGlyph);
                    }
                }
                catch
                {
                }

        // 2. Notify other components (like HomeViewModel)
        var isDark = true;
        if (Content is FrameworkElement root) isDark = root.ActualTheme == ElementTheme.Dark;
        WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(isDark));
    }

    private void Providers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update sidebar on collection change
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            foreach (SidebarProviderItem item in e.NewItems)
                AddProviderToSidebar(item);
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            foreach (SidebarProviderItem item in e.OldItems)
                RemoveProviderFromSidebar(item.Id);
        else if (e.Action == NotifyCollectionChangedAction.Reset) LoadProvidersToSidebar();
    }

    private void LoadProvidersToSidebar()
    {
        ClearDynamicProviders();
        foreach (var provider in ViewModel.Providers) AddProviderToSidebar(provider);
    }

    private void ClearDynamicProviders()
    {
        var menuItems = NavView.MenuItems;
        for (var i = menuItems.Count - 1; i >= 0; i--)
            if (menuItems[i] is NavigationViewItem item && item.Tag is string tag && tag.StartsWith("Provider_"))
                menuItems.RemoveAt(i);
    }

    private void AddProviderToSidebar(SidebarProviderItem provider)
    {
        var insertIndex = FindProvidersHeaderIndex();
        if (insertIndex == -1) return;

        // Create Icon using helper
        var icon = ResolveIconElement(provider.IconPath, provider.DefaultIconGlyph);

        var navItem = new NavigationViewItem
        {
            Content = provider.Name,
            Tag = $"Provider_{provider.Id}",
            Icon = icon
        };

        // Listen for changes
        provider.PropertyChanged += (s, e) =>
        {
            if (s is SidebarProviderItem p && navItem.Tag is string itemTag && itemTag == $"Provider_{p.Id}")
            {
                if (e.PropertyName == nameof(SidebarProviderItem.Name))
                    navItem.Content = p.Name;
                else if (e.PropertyName == nameof(SidebarProviderItem.IconPath) ||
                         e.PropertyName == nameof(SidebarProviderItem.Type))
                    navItem.Icon = ResolveIconElement(p.IconPath, p.DefaultIconGlyph);
            }
        };

        NavView.MenuItems.Insert(insertIndex, navItem);
    }

    private IconElement ResolveIconElement(string? iconPath, string defaultGlyph)
    {
        if (string.IsNullOrEmpty(iconPath)) return new FontIcon { Glyph = defaultGlyph };

        // 判断是否为预设名称
        if (ProviderIconHelper.IsPresetName(iconPath))
        {
            // 预设名称（如 "openai"）
            var isDark = true;
            if (Content is FrameworkElement root) isDark = root.ActualTheme == ElementTheme.Dark;

            var uri = ProviderIconHelper.GetPresetIconUri(iconPath, isDark);
            return new ImageIcon { Source = new BitmapImage(uri) };
        }

        // 自定义文件路径
        try
        {
            return new ImageIcon { Source = new BitmapImage(new Uri(iconPath)) };
        }
        catch
        {
            return new FontIcon { Glyph = defaultGlyph };
        }
    }

    private void RemoveProviderFromSidebar(Guid providerId)
    {
        var tag = $"Provider_{providerId}";
        var menuItems = NavView.MenuItems;
        for (var i = menuItems.Count - 1; i >= 0; i--)
            if (menuItems[i] is NavigationViewItem item && item.Tag is string itemTag && itemTag == tag)
            {
                menuItems.RemoveAt(i);
                break;
            }
    }

    private int FindProvidersHeaderIndex()
    {
        var menuItems = NavView.MenuItems;
        for (var i = 0; i < menuItems.Count; i++)
            if (menuItems[i] is NavigationViewItemHeader header && header.Tag?.ToString() == "ProvidersHeader")
            {
                // 找到所有已存在的 Provider_ 项，插入到最后一个之后
                var lastProviderIndex = i;
                for (var j = i + 1; j < menuItems.Count; j++)
                    if (menuItems[j] is NavigationViewItem item && item.Tag is string tag &&
                        tag.StartsWith("Provider_"))
                        lastProviderIndex = j;
                    else
                        break;

                return lastProviderIndex + 1;
            }

        return -1;
    }


    public void SelectProviderInSidebar(Guid providerId)
    {
        var tag = $"Provider_{providerId}";
        foreach (var item in NavView.MenuItems)
            if (item is NavigationViewItem navItem && navItem.Tag is string itemTag && itemTag == tag)
            {
                NavView.SelectedItem = navItem;
                break;
            }
    }

    private void TitleBar_OnBackRequested(TitleBar sender, object args)
    {
        if (ContentFrame.CanGoBack) ContentFrame.GoBack();
    }

    private void TitleBar_OnPaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
            if (tag == "AddProvider")
            {
                // 直接创建默认供应商
                var newProvider = App.ProviderService.CreateProvider();

                // 发送创建消息，触发侧边栏更新和导航
                WeakReferenceMessenger.Default.Send(new ProviderCreatedMessage(newProvider.Id));
            }
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

            if (tag.StartsWith("Provider_"))
            {
                var providerId = tag.Substring("Provider_".Length);
                ContentFrame.Navigate(typeof(ProviderSettingsPage), providerId);
                return;
            }

            var pageType = tag switch
            {
                "HomePage" => typeof(HomePage),
                "ModelsPage" => typeof(ModelsPage),
                "KeysPage" => typeof(KeysPage),
                _ => null
            };
            if (pageType != null)
                NavigateTo(pageType);
        }
    }

    private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
    {
        var canGoBack = ContentFrame?.CanGoBack ?? false;
        TitleBar.IsBackButtonVisible = canGoBack;

        // 同步侧边栏选中状态
        if (ContentFrame?.Content is Page page)
        {
            var pageType = page.GetType();

            // 如果是 ProviderSettingsPage，需要根据参数选中对应的服务商
            if (pageType == typeof(ProviderSettingsPage) && e.Parameter is string providerIdStr)
            {
                if (Guid.TryParse(providerIdStr, out var providerId)) SelectProviderInSidebar(providerId);
            }
            // 如果是其他页面，同步到对应的导航项
            else if (_pageToNavItem.TryGetValue(pageType, out var navItem))
            {
                NavView.SelectedItem = navItem;
            }
        }
    }

    private void NavigateTo(Type pageType)
    {
        if (ContentFrame.Content?.GetType() != pageType) // 避免重复导航
            ContentFrame.Navigate(pageType);

        // 同步 NavView 选中状态
        if (_pageToNavItem.TryGetValue(pageType, out var navItem))
            NavView.SelectedItem = navItem;
    }

    // ========== 搜索框事件处理 ==========

    private void SearchBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) SearchViewModel.PerformSearch(sender.Text);
    }

    private void SearchBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SearchResultItem item) sender.Text = item.Title;
    }

    private void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SearchResultItem item) HandleSearchResultSelected(item);
    }

    private async void HandleSearchResultSelected(SearchResultItem item)
    {
        switch (item.Type)
        {
            case SearchResultType.Provider:
                // 导航到服务商设置页
                if (item.Data is Guid providerId)
                {
                    ContentFrame.Navigate(typeof(ProviderSettingsPage), providerId.ToString());
                    SelectProviderInSidebar(providerId);
                }

                break;

            case SearchResultType.Model:
                // 导航到模型页，并设置搜索文本
                if (item.Data is string modelId)
                {
                    ContentFrame.Navigate(typeof(ModelsPage), modelId);
                    NavView.SelectedItem = ModelsPageItem;
                }

                break;

            case SearchResultType.SystemFile:
                // 打开文件
                if (item.Data is string filePath)
                    await CrashLogHelper.OpenFileWithDefaultEditorAsync(filePath, Content.XamlRoot);
                break;
        }

        // 清空搜索框并关闭下拉列表
        SearchBox.Text = string.Empty;
        SearchBox.IsSuggestionListOpen = false;
    }
}