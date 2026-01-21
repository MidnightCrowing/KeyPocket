using Microsoft.UI.Xaml.Controls;
using KeyPocket.UI.ViewModels;

namespace KeyPocket.UI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsViewModel();
        InitializeComponent();
    }
}