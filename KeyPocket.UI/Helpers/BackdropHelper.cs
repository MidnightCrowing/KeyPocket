using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace KeyPocket.UI.Helpers;

public enum WindowBackdropKind
{
    Mica,
    MicaAlt,
    Acrylic,
    None
}

public static class BackdropHelper
{
    public static void Apply(Window? window, WindowBackdropKind kind)
    {
        if (window == null) return;

        try
        {
            window.SystemBackdrop = kind switch
            {
                WindowBackdropKind.Mica => new MicaBackdrop(),
                WindowBackdropKind.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                WindowBackdropKind.Acrylic => new DesktopAcrylicBackdrop(),
                _ => null
            };
        }
        catch
        {
            window.SystemBackdrop = null;
        }
    }

    public static void ApplyToMainWindow(WindowBackdropKind kind)
    {
        Apply(WindowHelper.GetMainWindow(), kind);
    }
}
