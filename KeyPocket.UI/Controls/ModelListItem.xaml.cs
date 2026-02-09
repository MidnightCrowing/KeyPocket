using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KeyPocket.UI.Controls;

public sealed partial class ModelListItem : UserControl
{
    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register(
            nameof(Model),
            typeof(ModelItemViewModel),
            typeof(ModelListItem),
            new PropertyMetadata(null, OnModelChanged));

    public ModelListItem()
    {
        InitializeComponent();
    }

    public ModelItemViewModel? Model
    {
        get => (ModelItemViewModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModelListItem control)
        {
            control.Bindings.Update();
            control.UpdateHoverState(false);
        }
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateHoverState(true);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateHoverState(false);
    }

    private void UpdateHoverState(bool isHovered)
    {
        CopyBtn.SetValue(OpacityProperty, isHovered ? 1d : 0d);

        if (isHovered)
            FavoriteBtn.SetValue(OpacityProperty, 1d);
        else
            FavoriteBtn.ClearValue(OpacityProperty);
    }
}