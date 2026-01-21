using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.UI.Messages;
using KeyPocket.UI.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.ViewModels;

namespace KeyPocket.UI;

public sealed partial class MainWindow
{
    private readonly Dictionary<Type, NavigationViewItem> _pageToNavItem;
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);

        // 初始化 ViewModel
        ViewModel = new MainWindowViewModel(App.ProviderService);

        _pageToNavItem = new Dictionary<Type, NavigationViewItem>
        {
            { typeof(HomePage), homePage },
            { typeof(ModelsPage), modelsPageItem },
            { typeof(KeysPage), keysPageItem }
        };

        navView.SelectedItem = homePage;
        contentFrame.Navigate(typeof(HomePage));

        contentFrame.Navigated += OnContentFrameNavigated;
        
        // 初始化侧边栏服务商列表
        LoadProvidersToSidebar();
        
        // 监听 ViewModel 的 Providers 集合变化
        ViewModel.Providers.CollectionChanged += Providers_CollectionChanged;
        
        // 订阅服务商创建消息以处理导航
        WeakReferenceMessenger.Default.Register<ProviderCreatedMessage>(this, (r, m) =>
        {
            // 使用 Dispatcher 确保 Provider 已完全保存后再导航
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                // 导航到新创建的服务商配置页
                contentFrame.Navigate(typeof(ProviderSettingsPage), m.ProviderId.ToString());
                SelectProviderInSidebar(m.ProviderId);
            });
        });
    }

    private void Providers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // 当 ViewModel 的 Providers 集合变化时，更新侧边栏
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (SidebarProviderItem item in e.NewItems)
            {
                AddProviderToSidebar(item);
            }
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (SidebarProviderItem item in e.OldItems)
            {
                RemoveProviderFromSidebar(item.Id);
            }
        }
        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            LoadProvidersToSidebar();
        }
    }

    private void LoadProvidersToSidebar()
    {
        // 清除现有的动态服务商项
        ClearDynamicProviders();
        
        // 添加所有服务商
        foreach (var provider in ViewModel.Providers)
        {
            AddProviderToSidebar(provider);
        }
    }

    private void ClearDynamicProviders()
    {
        var menuItems = navView.MenuItems;
        for (int i = menuItems.Count - 1; i >= 0; i--)
        {
            if (menuItems[i] is NavigationViewItem item && item.Tag is string tag && tag.StartsWith("Provider_"))
            {
                menuItems.RemoveAt(i);
            }
        }
    }

    private void AddProviderToSidebar(SidebarProviderItem provider)
    {
        // 找到 "Providers" 标题的位置
        int insertIndex = FindProvidersHeaderIndex();
        if (insertIndex == -1) return;

        // 创建图标
        IconElement icon;
        if (!string.IsNullOrEmpty(provider.IconPath))
        {
            icon = new ImageIcon { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri($"ms-appdata:///local/{provider.IconPath}")) };
        }
        else
        {
            icon = new FontIcon { Glyph = provider.DefaultIconGlyph };
        }

        // 创建导航项
        var navItem = new NavigationViewItem
        {
            Content = provider.Name,
            Tag = $"Provider_{provider.Id}",
            Icon = icon
        };

        // 监听 provider 属性变化以更新 UI
        provider.PropertyChanged += (s, e) =>
        {
            if (s is SidebarProviderItem p && navItem.Tag is string itemTag && itemTag == $"Provider_{p.Id}")
            {
                if (e.PropertyName == nameof(SidebarProviderItem.Name))
                {
                    navItem.Content = p.Name;
                }
                else if (e.PropertyName == nameof(SidebarProviderItem.IconPath) || e.PropertyName == nameof(SidebarProviderItem.Type))
                {
                    // 更新图标
                    if (!string.IsNullOrEmpty(p.IconPath))
                    {
                        navItem.Icon = new ImageIcon { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri($"ms-appdata:///local/{p.IconPath}")) };
                    }
                    else
                    {
                        navItem.Icon = new FontIcon { Glyph = p.DefaultIconGlyph };
                    }
                }
            }
        };

        navView.MenuItems.Insert(insertIndex, navItem);
    }

    private void RemoveProviderFromSidebar(Guid providerId)
    {
        var tag = $"Provider_{providerId}";
        var menuItems = navView.MenuItems;
        for (int i = menuItems.Count - 1; i >= 0; i--)
        {
            if (menuItems[i] is NavigationViewItem item && item.Tag is string itemTag && itemTag == tag)
            {
                menuItems.RemoveAt(i);
                break;
            }
        }
    }

    private int FindProvidersHeaderIndex()
    {
        var menuItems = navView.MenuItems;
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (menuItems[i] is NavigationViewItemHeader header && header.Content?.ToString() == "Providers")
            {
                // 找到所有已存在的 Provider_ 项，插入到最后一个之后
                int lastProviderIndex = i;
                for (int j = i + 1; j < menuItems.Count; j++)
                {
                    if (menuItems[j] is NavigationViewItem item && item.Tag is string tag && tag.StartsWith("Provider_"))
                    {
                        lastProviderIndex = j;
                    }
                    else
                    {
                        break;
                    }
                }
                return lastProviderIndex + 1;
            }
        }
        return -1;
    }


    public void SelectProviderInSidebar(Guid providerId)
    {
        var tag = $"Provider_{providerId}";
        foreach (var item in navView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag is string itemTag && itemTag == tag)
            {
                navView.SelectedItem = navItem;
                break;
            }
        }
    }

    private void TitleBar_OnBackRequested(TitleBar sender, object args)
    {
        if (contentFrame.CanGoBack) contentFrame.GoBack();
    }

    private void TitleBar_OnPaneToggleRequested(TitleBar sender, object args)
    {
        navView.IsPaneOpen = !navView.IsPaneOpen;
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            if (tag == "AddProvider")
            {
                // 直接创建默认供应商
                var newProvider = App.ProviderService.CreateProvider();
                
                // 发送创建消息，触发侧边栏更新和导航
                WeakReferenceMessenger.Default.Send(new ProviderCreatedMessage(newProvider.Id));
            }
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
                contentFrame.Navigate(typeof(ProviderSettingsPage), providerId);
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
        var canGoBack = contentFrame?.CanGoBack ?? false;
        titleBar.IsBackButtonVisible = canGoBack;

        // 同步侧边栏选中状态
        if (contentFrame?.Content is Page page)
        {
            var pageType = page.GetType();
            
            // 如果是 ProviderSettingsPage，需要根据参数选中对应的服务商
            if (pageType == typeof(ProviderSettingsPage) && e.Parameter is string providerIdStr)
            {
                if (Guid.TryParse(providerIdStr, out var providerId))
                {
                    SelectProviderInSidebar(providerId);
                }
            }
            // 如果是其他页面，同步到对应的导航项
            else if (_pageToNavItem.TryGetValue(pageType, out var navItem))
            {
                navView.SelectedItem = navItem;
            }
        }
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
