// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace KeyPocket.UI.Helpers;

public static class ThemeHelper
{
    public static ElementTheme Theme
    {
        get
        {
            var window = WindowHelper.GetMainWindow();
            if (window?.Content is FrameworkElement rootElement)
            {
                return rootElement.RequestedTheme;
            }

            return ElementTheme.Default;
        }
        set
        {
            var window = WindowHelper.GetMainWindow();
            if (window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = value;
                TitleBarHelper.ApplySystemThemeToCaptionButtons(window, value);
            }
        
            SettingsHelper.Current.SelectedAppTheme = value;
        }
    }

    public static void Initialize()
    {
        Theme = SettingsHelper.Current.SelectedAppTheme;
    }

    public static bool IsDarkTheme()
    {
        if (Theme == ElementTheme.Default)
        {
            return Application.Current.RequestedTheme == ApplicationTheme.Dark;
        }
        return Theme == ElementTheme.Dark;
    }
}
