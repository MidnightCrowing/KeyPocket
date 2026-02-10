using System;
using System.ComponentModel;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI.Pages;

public sealed partial class KeysPage : Page
{
    public KeysPage()
    {
        InitializeComponent();
        ViewModel = new KeysViewModel(App.ProviderService);
        DataContext = ViewModel;
    }

    public KeysViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Reload data when navigated to ensure we have latest providers/keys
        ViewModel.LoadData();
        if (e.Parameter is string tag && !string.IsNullOrWhiteSpace(tag))
            ViewModel.SearchText = tag;
        UpdateTreeView();

        // Subscribe to property changes to update tree
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnNavigatedFrom(e);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KeysViewModel.GroupedKeys)) UpdateTreeView();
    }

    private void UpdateTreeView()
    {
        // If not loaded or view mode indicates it might not be ready, check null
        if (KeysTreeView == null) return;

        KeysTreeView.RootNodes.Clear();

        if (ViewModel.GroupedKeys == null) return;

        foreach (var group in ViewModel.GroupedKeys)
        {
            var groupNode = new TreeViewNode { Content = group, IsExpanded = true };

            foreach (var key in group.Keys)
            {
                var keyNode = new TreeViewNode { Content = key };
                groupNode.Children.Add(keyNode);
            }

            KeysTreeView.RootNodes.Add(groupNode);
        }
    }

    private void KeysTreeView_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTreeView();
    }

    private void OnProviderGroupDoubleTapped(object sender, Guid providerId)
    {
        if (providerId == Guid.Empty) return;
        Frame.Navigate(typeof(ProviderSettingsPage), providerId.ToString());
    }
}
