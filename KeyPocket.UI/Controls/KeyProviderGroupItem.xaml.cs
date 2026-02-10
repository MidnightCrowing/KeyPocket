using System;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Controls;

public sealed partial class KeyProviderGroupItem : UserControl
{
    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.Register(
            nameof(Group),
            typeof(KeyProviderGroupViewModel),
            typeof(KeyProviderGroupItem),
            new PropertyMetadata(null, OnGroupChanged));

    public KeyProviderGroupItem()
    {
        InitializeComponent();
    }

    public KeyProviderGroupViewModel? Group
    {
        get => (KeyProviderGroupViewModel?)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    public event EventHandler<Guid>? NavigateRequested;

    private static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyProviderGroupItem control) control.Bindings.Update();
    }

    private void OnDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (Group == null || Group.ProviderId == Guid.Empty) return;
        NavigateRequested?.Invoke(this, Group.ProviderId);
    }
}