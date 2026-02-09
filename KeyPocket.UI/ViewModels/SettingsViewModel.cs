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
        _themeIndex = ThemeHelper.Theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };

        var lang = ApplicationLanguages.PrimaryLanguageOverride;
        if (string.IsNullOrEmpty(lang))
            _languageIndex = 0;
        else if (lang.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            _languageIndex = 1;
        else if (lang.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                 lang.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase))
            _languageIndex = 2;
        else if (lang.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                 lang.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            _languageIndex = 3;
        else
            _languageIndex = 0;

        LoadCurrencies();
        _selectedCurrency = SettingsHelper.Current.SelectedCurrency;

        LoadRates();
    }

    public ObservableCollection<string> AvailableCurrencies { get; } = new();

    public ObservableCollection<object> Rates { get; } = new();

    public string Version =>
        ProcessInfoHelper.GetVersion() is Version version
            ? string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision)
            : string.Empty;
}
