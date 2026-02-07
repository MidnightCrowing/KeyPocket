using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Helpers;

public class ModelsItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ProviderTemplate { get; set; }
    public DataTemplate? ModelTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is TreeViewNode node) item = node.Content;

        if (item is ProviderGroupViewModel) return ProviderTemplate;

        if (item is ModelItemViewModel) return ModelTemplate;

        return base.SelectTemplateCore(item, container);
    }
}