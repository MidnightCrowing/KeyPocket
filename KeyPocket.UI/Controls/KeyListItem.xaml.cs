using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KeyPocket.UI.Controls;

public sealed partial class KeyListItem : UserControl
{
    public static readonly DependencyProperty KeyItemProperty =
        DependencyProperty.Register(
            nameof(KeyItem),
            typeof(KeyItemViewModel),
            typeof(KeyListItem),
            new PropertyMetadata(null, OnKeyItemChanged));

    public KeyListItem()
    {
        InitializeComponent();
    }

    public KeyItemViewModel? KeyItem
    {
        get => (KeyItemViewModel?)GetValue(KeyItemProperty);
        set => SetValue(KeyItemProperty, value);
    }

    private static void OnKeyItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyListItem control)
        {
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
        if (CopyBtn != null) CopyBtn.Opacity = isHovered ? 1 : 0;

        if (FavoriteBtn == null) return;

        if (isHovered)
            FavoriteBtn.Opacity = 1;
        else
            FavoriteBtn.Opacity = KeyItem?.IsFavorite == true ? 1 : 0;
    }

    private void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        if (KeyItem == null) return;

        var decryptedKey = App.ProviderService.GetDecryptedApiKey(KeyItem.ProviderId, KeyItem.Id);
        if (string.IsNullOrEmpty(decryptedKey)) return;

        var package = new DataPackage();
        package.SetText(decryptedKey);
        Clipboard.SetContent(package);
    }

    private void TagTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (KeyItem == null) return;

        if (e.Key == VirtualKey.Enter)
        {
            KeyItem.CommitEditTagCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Escape)
        {
            KeyItem.CancelEditTagCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void TagTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus(FocusState.Programmatic);
            textBox.SelectAll();
        }
    }

    private void TagTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (KeyItem == null) return;
        if (KeyItem.IsEditingTag) KeyItem.CommitEditTagCommand.Execute(null);
    }
}