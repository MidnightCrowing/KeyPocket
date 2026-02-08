using System;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.WinUI.Controls;
using KeyPocket.Core.Services;
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

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
         // Copy logic is handled by command or direct clipboard
        if (sender is Button button && button.Tag is Guid keyId)
        {
             // Find the key and decrypt it
             // Since we might be in Card or List, checking ViewModel.FilteredKeys is safest.
             // Actually, KeyItemViewModel has logic to decrypt and mask, but we want the RAW key for copy.
             // Helper logic in previous KeysPage uses ProviderService.
             
            var keyItem = ViewModel.FilteredKeys.FirstOrDefault(k => k.Id == keyId);
            if (keyItem != null)
            {
                var decryptedKey = App.ProviderService.GetDecryptedApiKey(keyItem.ProviderId, keyId);
                if (!string.IsNullOrEmpty(decryptedKey))
                {
                    var package = new DataPackage();
                    package.SetText(decryptedKey);
                    Clipboard.SetContent(package);
                }
            }
        }
    }

    private void TagTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is KeyItemViewModel vm)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                vm.CommitEditTagCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Escape)
            {
                vm.CancelEditTagCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void TagTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus(FocusState.Programmatic);
            textBox.SelectAll();
        }
    }

    private void TagTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is KeyItemViewModel vm)
        {
            if (vm.IsEditingTag)
            {
                vm.CommitEditTagCommand.Execute(null);
            }
        }
    }

    private void KeyItem_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            UpdateButtonsVisibility(grid, true);
        }
    }

    private void KeyItem_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            UpdateButtonsVisibility(grid, false);
        }
    }

    private void KeyCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Child is StackPanel panel)
        {
            UpdateButtonsVisibility(panel, true);
        }
    }

    private void KeyCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Child is StackPanel panel)
        {
            UpdateButtonsVisibility(panel, false);
        }
    }

    private void UpdateButtonsVisibility(DependencyObject container, bool isHovered)
    {
        // Recursively find buttons by specific names
        var copyBtn = FindChildByName(container, "CopyBtn") ?? FindChildByName(container, "CardCopyBtn");
        var favoriteBtn = FindChildByName(container, "FavoriteBtn") ?? FindChildByName(container, "CardFavoriteBtn");

        if (copyBtn is Button cBtn)
        {
            cBtn.Opacity = isHovered ? 1 : 0;
        }

        if (favoriteBtn is Button fBtn)
        {
            if (isHovered)
            {
                fBtn.Opacity = 1;
            }
            else
            {
                // If not hovered, opacity depends on IsFavorite state
                // Extract ViewModel from DataContext
                var vm = fBtn.DataContext as KeyItemViewModel;
                if (vm != null)
                {
                    fBtn.Opacity = vm.IsFavorite ? 1 : 0;
                }
                else
                {
                    // Fallback for TreeView nodes (Content property)
                    var node = fBtn.DataContext as TreeViewNode;
                    if (node?.Content is KeyItemViewModel nodeVm)
                    {
                        fBtn.Opacity = nodeVm.IsFavorite ? 1 : 0;
                    }
                }
            }
        }
    }

    private FrameworkElement? FindChildByName(DependencyObject parent, string name)
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement element && element.Name == name)
            {
                return element;
            }
            var result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void KeysTreeView_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTreeView();
    }
}