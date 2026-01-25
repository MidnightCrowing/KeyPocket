using Microsoft.UI.Xaml.Controls;
using KeyPocket.UI.ViewModels;
using Windows.Globalization.NumberFormatting;

namespace KeyPocket.UI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsViewModel();
        InitializeComponent();
        ConfigureRateFormatter();
    }

    private void ConfigureRateFormatter()
    {
        if (Resources["RateFormatter"] is DecimalFormatter formatter)
        {
            var rounder = new IncrementNumberRounder();
            rounder.Increment = 0.01;
            rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;
            formatter.NumberRounder = rounder;
        }
    }
}