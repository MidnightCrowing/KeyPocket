using System;
using Microsoft.UI.Xaml.Data;

namespace KeyPocket.UI.Converters;

public class BoolToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool state = value is bool b && b;
        string? param = parameter as string;

        if (string.Equals(param, "Reveal", StringComparison.OrdinalIgnoreCase))
        {
            // If revealed (true), show standard Eye (indicating it's visible, or action to hide? 
            // Usually simpler: True = Visible State. Icon often represents the state.)
            // WinUI PasswordBox uses "Reveal" button. 
            // Let's stick to: True (Revealed) -> Eye (Open), False (Hidden) -> Eye with Line/Hidden.
            // Wait, usually the button is "Show Password". So if hidden, show "Eye". If visible, show "EyeHide".
            return state ? "\uED1A" : "\uE890"; // \uED1A = Hide, \uE890 = Show/Eye
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
