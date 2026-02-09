using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Controls;

public sealed partial class ModelCardItem : UserControl
{
    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register(
            nameof(Model),
            typeof(ModelItemViewModel),
            typeof(ModelCardItem),
            new PropertyMetadata(null));

    public ModelCardItem()
    {
        InitializeComponent();
    }

    public ModelItemViewModel? Model
    {
        get => (ModelItemViewModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        CopyBtn.SetValue(OpacityProperty, 1d);
        FavoriteBtn.SetValue(OpacityProperty, 1d);
    }

    private void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        CopyBtn.SetValue(OpacityProperty, 0d);
        FavoriteBtn.ClearValue(OpacityProperty);
    }
}
