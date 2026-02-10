using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Controls;

public sealed partial class KeyTag : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(KeyTag),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty TagCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(TagCornerRadius),
            typeof(CornerRadius),
            typeof(KeyTag),
            new PropertyMetadata(new CornerRadius(4)));

    public static readonly DependencyProperty TagPaddingProperty =
        DependencyProperty.Register(
            nameof(TagPadding),
            typeof(Thickness),
            typeof(KeyTag),
            new PropertyMetadata(new Thickness(6, 2, 6, 2)));

    public KeyTag()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string TagColor => TagHelper.GetTagColor(Text);

    public CornerRadius TagCornerRadius
    {
        get => (CornerRadius)GetValue(TagCornerRadiusProperty);
        set => SetValue(TagCornerRadiusProperty, value);
    }

    public Thickness TagPadding
    {
        get => (Thickness)GetValue(TagPaddingProperty);
        set => SetValue(TagPaddingProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyTag control)
        {
            control.Bindings.Update();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged += OnActualThemeChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ActualThemeChanged -= OnActualThemeChanged;
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        Bindings.Update();
    }
}
