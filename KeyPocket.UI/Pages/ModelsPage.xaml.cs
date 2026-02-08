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
            var copyBtn = FindDescendant<Button>(grid, "ListCopyBtn");
            var favBtn = FindDescendant<Button>(grid, "ListFavoriteBtn");

            if (favBtn != null) favBtn.Opacity = 1;
            if (copyBtn != null) copyBtn.Opacity = 1;
        }
    }

    private void ModelItem_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            var copyBtn = FindDescendant<Button>(grid, "ListCopyBtn");
            var favBtn = FindDescendant<Button>(grid, "ListFavoriteBtn");

            if (copyBtn != null) copyBtn.Opacity = 0;
            if (favBtn != null) favBtn.ClearValue(OpacityProperty);
        }
    }

    private void ModelCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            var favBtn = FindDescendant<Button>(border, "CardFavoriteBtn");
            var copyBtn = FindDescendant<Button>(border, "CardCopyBtn");

            if (favBtn != null) favBtn.Opacity = 1;
            if (copyBtn != null) copyBtn.Opacity = 1;
        }
    }

    private void ModelCard_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            var favBtn = FindDescendant<Button>(border, "CardFavoriteBtn");
            var copyBtn = FindDescendant<Button>(border, "CardCopyBtn");

            if (copyBtn != null) copyBtn.Opacity = 0;
            if (favBtn != null) favBtn.ClearValue(OpacityProperty);
        }
    }

    private T? FindDescendant<T>(DependencyObject element, string name) where T : FrameworkElement
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);
            if (child is T t && t.Name == name) return t;
            var result = FindDescendant<T>(child, name);
            if (result != null) return result;
        }
        return null;
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