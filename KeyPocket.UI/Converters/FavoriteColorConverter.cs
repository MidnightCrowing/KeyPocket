using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace KeyPocket.UI.Converters;

public class FavoriteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isFavorite && isFavorite) return new SolidColorBrush(Colors.Gold);

        // Return a default color or UnsetValue to use the control's default foreground
        // Using Gray for inactive/non-favorite state to be distinct but subtle
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}