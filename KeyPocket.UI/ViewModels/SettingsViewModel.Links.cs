using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Windows.System;

namespace KeyPocket.UI.ViewModels;

public partial class SettingsViewModel
{
    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPocket"));
    }

    [RelayCommand]
    private async Task OpenFeedbackAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPocket/issues"));
    }
}
