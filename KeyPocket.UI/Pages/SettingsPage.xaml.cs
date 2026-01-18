using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Pages;

public sealed partial class SettingsPage
{
    public string Version
    {
        get
        {
            return ProcessInfoHelper.GetVersion() is Version version
                ? string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision)
                : string.Empty;
        }
    }
    
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnSettingsPageLoaded;
        
        gitHubCard.Click += (_, _) =>
        {
            _ = Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPort"));
        };

        feedbackCard.Click += (_, _) =>
        {
            _ = Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPort/issues"));
        };
    }

    private void OnSettingsPageLoaded(object sender, RoutedEventArgs e)
    {
        // Set theme
        var currentTheme = ThemeHelper.Theme;
        themeMode.SelectedIndex = currentTheme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };
    }

    private void themeMode_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not UIElement senderUiLement ||
            (themeMode.SelectedItem as ComboBoxItem)?.Tag.ToString() is not string selectedTheme ||
            WindowHelper.GetWindowForElement(this) is not Window window)
        {
            return;
        }

        ThemeHelper.Theme = EnumHelper.GetEnum<ElementTheme>(selectedTheme);
    }
}