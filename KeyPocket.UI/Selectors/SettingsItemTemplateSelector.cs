using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using KeyPocket.UI.ViewModels;

namespace KeyPocket.UI.Selectors;

public class SettingsItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate RateTemplate { get; set; } = null!;
    public DataTemplate AddFooterTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is ExchangeRateItem)
        {
            return RateTemplate;
        }
        
        return AddFooterTemplate;
    }
}
