using System;
using System.Collections.ObjectModel;
using Windows.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;

namespace KeyPocket.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel()
    {
        ThemeIndex = ThemeHelper.Theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };

        BackdropIndex = SettingsHelper.Current.SelectedBackdrop switch
        {
            WindowBackdropKind.Mica => 0,
            WindowBackdropKind.MicaAlt => 1,
            WindowBackdropKind.Acrylic => 2,
            _ => 3
        };

        var lang = ApplicationLanguages.PrimaryLanguageOverride;
        if (string.IsNullOrEmpty(lang))
            LanguageIndex = 0;
        else if (lang.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            LanguageIndex = 1;
        else if (lang.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                 lang.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase))
            LanguageIndex = 2;
        else if (lang.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                 lang.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            LanguageIndex = 3;
        else
            LanguageIndex = 0;

        LoadCurrencies();
        SelectedCurrency = SettingsHelper.Current.SelectedCurrency;

        LoadRates();
    }

    public ObservableCollection<string> AvailableCurrencies { get; } = new();

    public ObservableCollection<object> Rates { get; } = new();

    public string Version =>
        ProcessInfoHelper.GetVersion() is Version version
            ? string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision)
            : string.Empty;
}
