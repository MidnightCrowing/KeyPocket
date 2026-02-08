using System;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using KeyPocket.Core.Services;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI.Pages;

public sealed partial class ModelsPage : Page
{
    public ModelsPage()
    {
        InitializeComponent();
        ViewModel = new ModelsViewModel(App.ProviderService, App.ModelFilterService);
        DataContext = ViewModel;
    }

    public ModelsViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Reload data when navigated to ensure we have latest providers/models
        ViewModel.LoadData();
        UpdateTreeView();

        // Subscribe to property changes to update tree
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // 如果提供了搜索参数（模型 ID），设置搜索文本
        if (e.Parameter is string modelId && !string.IsNullOrEmpty(modelId)) ViewModel.SearchText = modelId;

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnNavigatedFrom(e);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModelsViewModel.GroupedModels)) UpdateTreeView();
    }

    private void UpdateTreeView()
    {
        ModelsTreeView.RootNodes.Clear();

        if (ViewModel.GroupedModels == null) return;

        foreach (var group in ViewModel.GroupedModels)
        {
            var groupNode = new TreeViewNode { Content = group, IsExpanded = true };

            foreach (var model in group.Models)
            {
                var modelNode = new TreeViewNode { Content = model };
                groupNode.Children.Add(modelNode);
            }

            ModelsTreeView.RootNodes.Add(groupNode);
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        // Copy logic is handled by command or direct clipboard
        if (sender is Button button && button.Tag is string text)
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
    }

    private void ModelItem_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            var children = grid.Children;
            Button? copyBtn = null;
            Button? favBtn = null;

            foreach (var child in children)
                if (child is Button btn)
                {
                    var col = Grid.GetColumn(btn);
                    if (col == 2) copyBtn = btn;
                    else if (col == 4) favBtn = btn;
                }

            if (favBtn != null) favBtn.SetValue(OpacityProperty, 1d);
            if (copyBtn != null) copyBtn.SetValue(OpacityProperty, 1d);
        }
    }

    private void ModelItem_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            var children = grid.Children;
            Button? copyBtn = null;
            Button? favBtn = null;

            foreach (var child in children)
                if (child is Button btn)
                {
                    var col = Grid.GetColumn(btn);
                    if (col == 2) copyBtn = btn;
                    else if (col == 4) favBtn = btn;
                }

            if (copyBtn != null) copyBtn.SetValue(OpacityProperty, 0d);
            if (favBtn != null)
            {
                var isFavorite = false;
                if (grid.DataContext is TreeViewNode node && node.Content is ModelItemViewModel vm)
                    isFavorite = vm.IsFavorite;
                favBtn.SetValue(OpacityProperty, isFavorite ? 1d : 0d);
            }
        }
    }

    private void ModelCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            Button? copyBtn = null;
            Button? favBtn = null;

            foreach (var child in grid.Children)
                if (child is Button btn)
                {
                    var col = Grid.GetColumn(btn);
                    if (col == 2) favBtn = btn;
                    else if (col == 3) copyBtn = btn;
                }

            if (favBtn != null) favBtn.SetValue(OpacityProperty, 1d);
            if (copyBtn != null) copyBtn.SetValue(OpacityProperty, 1d);
        }
    }

    private void ModelCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            Button? copyBtn = null;
            Button? favBtn = null;

            foreach (var child in grid.Children)
                if (child is Button btn)
                {
                    var col = Grid.GetColumn(btn);
                    if (col == 2) favBtn = btn;
                    else if (col == 3) copyBtn = btn;
                }

            if (copyBtn != null) copyBtn.SetValue(OpacityProperty, 0d);
            if (favBtn != null)
            {
                var isFavorite = false;
                if (grid.DataContext is ModelItemViewModel vm) isFavorite = vm.IsFavorite;
                favBtn.SetValue(OpacityProperty, isFavorite ? 1d : 0d);
            }
        }
    }

    private void SortOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string tag)
            ViewModel.SortOption = Enum.Parse<ModelSortOption>(tag);
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetFilters();
    }

    private void ProviderGroup_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TreeViewNode node &&
            node.Content is ProviderGroupViewModel group && group.ProviderId != Guid.Empty)
            Frame.Navigate(typeof(ProviderSettingsPage), group.ProviderId.ToString());
    }
}