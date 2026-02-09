using System.ComponentModel;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KeyPocket.UI.Controls;

public sealed partial class ModelCardItem : UserControl
{
    private ModelItemViewModel? _currentModel;
    public static readonly DependencyProperty ModelProperty =
        DependencyProperty.Register(
            nameof(Model),
            typeof(ModelItemViewModel),
            typeof(ModelCardItem),
            new PropertyMetadata(null, OnModelChanged));

    public ModelCardItem()
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
        if (d is ModelCardItem control)
        {
            if (e.OldValue is ModelItemViewModel oldVm)
                oldVm.PropertyChanged -= control.OnModelPropertyChanged;

            if (e.NewValue is ModelItemViewModel newVm)
                newVm.PropertyChanged += control.OnModelPropertyChanged;

            control._currentModel = e.NewValue as ModelItemViewModel;
            control.Bindings.Update();
            control.UpdateButtonsVisibility(false);
        }
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateButtonsVisibility(true);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateButtonsVisibility(false);
    }

    private void UpdateButtonsVisibility(bool isHovered)
    {
        CopyBtn.SetValue(OpacityProperty, isHovered ? 1d : 0d);

        if (isHovered)
            FavoriteBtn.SetValue(OpacityProperty, 1d);
        else
            FavoriteBtn.SetValue(OpacityProperty, Model?.IsFavorite == true ? 1d : 0d);
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModelItemViewModel.IsFavorite))
            UpdateButtonsVisibility(false);
    }
}
