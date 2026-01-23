using Microsoft.UI.Xaml.Controls;
using KeyPocket.UI.ViewModels;
using System;
using System.Diagnostics;

namespace KeyPocket.UI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsViewModel();
        InitializeComponent();
    }

    private async void RefreshRateButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            Debug.WriteLine("RefreshRateButton_Click: invoking ViewModel.RefreshExchangeRateCommand");
            if (ViewModel.RefreshExchangeRateCommand.CanExecute(null))
            {
                await ViewModel.RefreshExchangeRateCommand.ExecuteAsync(null);
            }
            else
            {
                Debug.WriteLine("RefreshRateButton_Click: command cannot execute");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RefreshRateButton_Click: exception {ex}");
        }
    }
}