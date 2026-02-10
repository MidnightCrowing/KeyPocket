using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Pages.Models;

public sealed partial class ModelsFilterButton : UserControl
{
    public ModelsFilterButton()
    {
        InitializeComponent();
    }

    public ModelsViewModel? ViewModel => DataContext as ModelsViewModel;

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.ResetFilters();
    }
}
