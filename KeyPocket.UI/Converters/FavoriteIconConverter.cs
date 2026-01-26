using System;
using Microsoft.UI.Xaml.Data;

namespace KeyPocket.UI.Converters;

public class FavoriteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // True = Solid Star (Favorite), False = Outline Star (Not Favorite)
        return (value is bool isFavorite && isFavorite) ? "\uE735" : "\uE734";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
