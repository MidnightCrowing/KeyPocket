using System;
using Microsoft.UI.Xaml.Data;

namespace KeyPocket.UI.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null)
            return false;

        string checkValue = value.ToString();
        string targetValue = parameter.ToString();
        return checkValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue && boolValue)
            return Enum.Parse(targetType, parameter.ToString());
        return null;
    }
}
