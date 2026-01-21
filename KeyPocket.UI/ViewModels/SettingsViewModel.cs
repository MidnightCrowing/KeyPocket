using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;
using System;
using Windows.System;

#pragma warning disable MVVMTK0045

namespace KeyPocket.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _themeIndex;

    public string Version
    {
        get
        {
            return ProcessInfoHelper.GetVersion() is Version version
                ? string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision)
                : string.Empty;
        }
    }

    public SettingsViewModel()
    {
        // 根据当前主题设置索引
        _themeIndex = ThemeHelper.Theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };
    }

    partial void OnThemeIndexChanged(int value)
    {
        // 根据索引设置主题
        ThemeHelper.Theme = value switch
        {
            0 => ElementTheme.Light,
            1 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task OpenGitHubAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPocket"));
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task OpenFeedbackAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPocket/issues"));
    }
}
