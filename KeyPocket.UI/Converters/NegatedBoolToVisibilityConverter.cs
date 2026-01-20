using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace KeyPocket.UI.Converters;

public class NegatedBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility v)
        {
            return v == Visibility.Collapsed;
        }
        return false;
    }
}
