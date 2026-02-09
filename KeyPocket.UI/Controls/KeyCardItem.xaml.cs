using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KeyPocket.UI.Controls;

public sealed partial class KeyCardItem : UserControl
{
    private KeyItemViewModel? _currentKeyItem;
    public static readonly DependencyProperty KeyItemProperty =
        DependencyProperty.Register(
            nameof(KeyItem),
            typeof(KeyItemViewModel),
            typeof(KeyCardItem),
            new PropertyMetadata(null, OnKeyItemChanged));

    public KeyCardItem()
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
        if (d is KeyCardItem control)
        {
            if (e.OldValue is KeyItemViewModel oldVm)
                oldVm.PropertyChanged -= control.OnKeyItemPropertyChanged;

            if (e.NewValue is KeyItemViewModel newVm)
                newVm.PropertyChanged += control.OnKeyItemPropertyChanged;

            control._currentKeyItem = e.NewValue as KeyItemViewModel;
            control.Bindings.Update();
            control.UpdateButtonsVisibility(false);
            control.UpdateTagBorderVisibility(false);
        }
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateButtonsVisibility(true);
        UpdateTagBorderVisibility(true);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateButtonsVisibility(false);
        UpdateTagBorderVisibility(false);
    }

    private void OnTagColumnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateTagBorderVisibility(true);
    }

    private void OnTagColumnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateTagBorderVisibility(false);
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

    private void UpdateTagBorderVisibility(bool isHovered)
    {
        if (TagBorder == null || KeyItem == null) return;

        if (KeyItem.IsEditingTag)
        {
            TagBorder.Visibility = Visibility.Collapsed;
            return;
        }

        TagBorder.Visibility = KeyItem.HasTag ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnKeyItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KeyItemViewModel.Tag) ||
            e.PropertyName == nameof(KeyItemViewModel.IsEditingTag))
        {
            UpdateTagBorderVisibility(false);
            if (KeyItem?.IsEditingTag == true)
                FocusTagTextBox();
        }
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
        if (sender is TextBox) FocusTagTextBox();
    }

    private void TagTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (KeyItem == null) return;
        if (KeyItem.IsEditingTag) KeyItem.CommitEditTagCommand.Execute(null);
    }

    private void FocusTagTextBox()
    {
        if (TagTextBox == null) return;
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            TagTextBox.Focus(FocusState.Programmatic);
            TagTextBox.SelectAll();
        });
    }
}
