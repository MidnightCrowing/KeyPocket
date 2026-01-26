using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace KeyPocket.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Parameter supports "Inverse" or similar if needed, but for now simple true=Visible
        var result = value is bool b && b;
        if (parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            result = !result;

        return result ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}