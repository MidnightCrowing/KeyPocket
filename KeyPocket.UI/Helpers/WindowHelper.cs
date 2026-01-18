// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace KeyPocket.UI.Helpers;

// Helper class to allow the app to find the Window that contains an
// arbitrary UIElement (GetWindowForElement).  To do this, we keep track
// of all active Windows.  The app code must call WindowHelper.CreateWindow
// rather than "new Window" so we can keep track of all the relevant
// windows.  In the future, we would like to support this in platform APIs.
public static class WindowHelper
{
    private static Window? _mainWindow;

    public static void SetMainWindow(Window window) => _mainWindow = window;

    public static Window? GetMainWindow() => _mainWindow;

    public static Window? GetWindowForElement(UIElement element)
    {
        if (element.XamlRoot == null || _mainWindow?.Content?.XamlRoot == null) return null;
        return element.XamlRoot == _mainWindow.Content.XamlRoot ? _mainWindow : null;
    }

    // get dpi for an element
    public static double GetRasterizationScaleForElement(UIElement element)
    {
        return element.XamlRoot?.RasterizationScale ?? 0.0;
    }

    public static void SetWindowMinSize(Window window, double width, double height)
    {
        if (window.Content is not FrameworkElement windowContent) return;
        if (windowContent.XamlRoot is null) return;
        if (window.AppWindow.Presenter is not OverlappedPresenter presenter) return;

        var scale = windowContent.XamlRoot.RasterizationScale;
        presenter.PreferredMinimumWidth = (int)(width * scale);
        presenter.PreferredMinimumHeight = (int)(height * scale);
    }
}
