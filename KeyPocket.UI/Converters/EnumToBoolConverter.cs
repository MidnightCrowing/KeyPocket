using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace KeyPocket.UI.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null)
            return false;

        var checkValue = value.ToString();
        var targetValue = parameter.ToString();

        if (checkValue == null || targetValue == null) return false;

        return checkValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue && boolValue && parameter != null)
            try
            {
                return Enum.Parse(targetType, parameter.ToString()!);
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }

        return DependencyProperty.UnsetValue;
    }
}