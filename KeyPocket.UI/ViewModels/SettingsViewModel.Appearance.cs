using Windows.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;

namespace KeyPocket.UI.ViewModels;

public partial class SettingsViewModel
{
    [ObservableProperty] public partial int BackdropIndex { get; set; }

    [ObservableProperty] public partial int LanguageIndex { get; set; }

    [ObservableProperty] public partial bool ShowRestartWarning { get; set; }

    [ObservableProperty] public partial int ThemeIndex { get; set; }

    partial void OnThemeIndexChanged(int value)
    {
        ThemeHelper.Theme = value switch
        {
            0 => ElementTheme.Light,
            1 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    partial void OnBackdropIndexChanged(int value)
    {
        var kind = value switch
        {
            0 => WindowBackdropKind.Mica,
            1 => WindowBackdropKind.MicaAlt,
            2 => WindowBackdropKind.Acrylic,
            _ => WindowBackdropKind.None
        };

        SettingsHelper.Current.SelectedBackdrop = kind;
        BackdropHelper.ApplyToMainWindow(kind);
    }

    partial void OnLanguageIndexChanged(int value)
    {
        var newLang = value switch
        {
            1 => "en-US",
            2 => "zh-CN",
            3 => "zh-TW",
            _ => string.Empty
        };

        if (ApplicationLanguages.PrimaryLanguageOverride != newLang)
        {
            ApplicationLanguages.PrimaryLanguageOverride = newLang;
            ShowRestartWarning = true;
        }
    }
}
