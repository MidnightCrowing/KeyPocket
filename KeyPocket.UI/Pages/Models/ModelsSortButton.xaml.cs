using System;
using KeyPocket.Core.Services;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace KeyPocket.UI.Pages.Models;

public sealed partial class ModelsSortButton : UserControl
{
    public ModelsSortButton()
    {
        InitializeComponent();
    }

    public ModelsViewModel? ViewModel => DataContext as ModelsViewModel;

    private void SortOption_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        if (sender is MenuFlyoutItem item && item.Tag is string tag)
            ViewModel.SortOption = Enum.Parse<ModelSortOption>(tag);
    }
}
