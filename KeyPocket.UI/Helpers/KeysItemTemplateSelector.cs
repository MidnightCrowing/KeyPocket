using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Helpers;

public class KeysItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ProviderTemplate { get; set; }
    public DataTemplate? KeyTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is TreeViewNode node) item = node.Content;

        if (item is KeyProviderGroupViewModel && ProviderTemplate != null) return ProviderTemplate;

        if (item is KeyItemViewModel && KeyTemplate != null) return KeyTemplate;

        return base.SelectTemplateCore(item, container);
    }
}
