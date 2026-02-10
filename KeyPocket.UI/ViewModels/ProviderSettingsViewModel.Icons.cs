using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Messages;
using Microsoft.UI.Dispatching;

namespace KeyPocket.UI.ViewModels;

public partial class ProviderSettingsViewModel : ObservableObject
{
    private async void LoadDefaultIcons()
    {
        // Capture dispatcher from UI thread
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Capture theme on UI thread
        var isDark = ThemeHelper.IsDarkTheme();

        await Task.Run(() =>
        {
            var iconNames = ProviderIconHelper.GetAllPresetIconNames();

            dispatcherQueue?.TryEnqueue(() =>
            {
                DefaultIcons.Clear();
                foreach (var iconName in iconNames)
                {
                    var displayName = iconName.Length > 0
                        ? char.ToUpper(iconName[0]) + iconName.Substring(1)
                        : iconName;

                    DefaultIcons.Add(new DefaultIconItem
                    {
                        Name = displayName,
                        Path = ProviderIconHelper.GetPresetIconUri(iconName, isDark).ToString(),
                        FileName = iconName
                    });
                }
            });
        });
    }

    [RelayCommand]
    public async Task SelectDefaultIcon(DefaultIconItem? item)
    {
        if (item == null || _providerService == null) return;

        try
        {
            // When selecting a preset, we just save the name!
            // The main window will resolve it to Assets/ProviderIcons/{Name}-{Theme}.png

            // Save the preset base name so theme-specific assets can resolve correctly
            var newPath = string.IsNullOrWhiteSpace(item.FileName) ? item.Name : item.FileName;

            Provider.IconPath = newPath;
            _providerService.UpdateProviderIcon(Provider.Id, newPath);

            // Update property
            HasCustomIcon = !string.IsNullOrEmpty(Provider.IconPath);

            // Notify sidebar
            WeakReferenceMessenger.Default.Send(new ProviderUpdatedMessage(Provider.Id));
        }
        catch (Exception)
        {
            // Logging?
        }
    }

    private string FormatDefaultModelName(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return id;
        // Convert to Title Case (e.g. "gpt-4" -> "Gpt-4", "deepseek" -> "Deepseek")
        // ToLower() first to ensure ToTitleCase processes it correctly even if input is ALLCAPS or mixed.
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.ToLower());
    }
}