using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.UI.Messages;
using KeyPocket.UI.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using KeyPocket.UI.Dialogs;
using KeyPocket.UI.Helpers;

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
            { typeof(ModelsPage), modelsPageItem },
            { typeof(KeysPage), keysPageItem }
        };

        LoadProvidersToSidebar();

        navView.SelectedItem = homePage;
        contentFrame.Navigate(typeof(HomePage));

        contentFrame.Navigated += OnContentFrameNavigated;
        
        // Register for provider deletion messages
        WeakReferenceMessenger.Default.Register<ProviderDeletedMessage>(this, (r, m) =>
        {
            LoadProvidersToSidebar();
        });
    }

    private void LoadProvidersToSidebar()
    {
        // Clear existing dynamic providers (Tag starts with Provider_)
        var menuItems = navView.MenuItems;
        for (int i = menuItems.Count - 1; i >= 0; i--)
        {
            if (menuItems[i] is NavigationViewItem item && item.Tag is string tag && tag.StartsWith("Provider_"))
            {
                menuItems.RemoveAt(i);
            }
        }

        // Find "Providers" header index
        int insertIndex = -1;
        for(int i=0; i<menuItems.Count; i++)
        {
            if (menuItems[i] is NavigationViewItemHeader header && header.Content?.ToString() == "Providers")
            {
                insertIndex = i + 1;
                break;
            }
        }

        if (insertIndex == -1) return; // Header not found

        // Add Provider Items
        var providers = App.ProviderService.GetAllProviders();
        foreach (var p in providers)
        {
            IconElement icon;
            if (!string.IsNullOrEmpty(p.IconPath))
            {
               icon = new ImageIcon { Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri($"ms-appdata:///local/{p.IconPath}")) };
            }
            else
            {
               icon = new FontIcon { Glyph = p.Type switch
                {
                    _ => "\uE99A" // Unified default icon
                } };
            }

            var item = new NavigationViewItem
            {
                Content = p.Name,
                Tag = $"Provider_{p.Id}",
                Icon = icon
            };
            menuItems.Insert(insertIndex++, item);
        }
    }

    private string GetIconForType(string type)
    {
        return type switch
        {
            "OpenAI Compatible" => "\uE80F",
            "Anthropic" => "\uF158",
            _ => "\uE774"
        };
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

    private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            if (tag == "AddProvider")
            {
                var dialog = new AddProviderDialog();
                dialog.RequestedTheme = ThemeHelper.IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.XamlRoot = this.Content.XamlRoot;
                var result = await dialog.ShowAsync();
                
                // Refresh Sidebar after adding
                if (result == ContentDialogResult.Primary)
                {
                    App.ProviderService.CreateProvider(
                        dialog.ProviderName,
                        dialog.ProviderType,
                        dialog.BaseUrl,
                        dialog.Description
                    );
                    
                    LoadProvidersToSidebar();
                }
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

        if (contentFrame?.Content is Page page)
        {
             // Here we could try to sync selection back, but dynamic IDs are tricky.
             // Usually irrelevant for simple back navigation in this scope.
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