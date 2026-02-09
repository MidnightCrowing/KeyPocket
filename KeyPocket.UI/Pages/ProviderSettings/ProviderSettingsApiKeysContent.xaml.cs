using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KeyPocket.UI.Pages.ProviderSettings;

public sealed partial class ProviderSettingsApiKeysContent : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ProviderSettingsViewModel),
            typeof(ProviderSettingsApiKeysContent),
            new PropertyMetadata(null));

    public ProviderSettingsApiKeysContent()
    {
        InitializeComponent();
    }

    public ProviderSettingsViewModel? ViewModel
    {
        get => (ProviderSettingsViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private void OnKeyTagKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && sender is TextBox tb && tb.DataContext is KeyWrapper wrapper)
        {
            wrapper.CommitTagEditCommand?.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Escape && sender is TextBox tb2 && tb2.DataContext is KeyWrapper wrapper2)
        {
            wrapper2.CancelTagEditCommand?.Execute(null);
            e.Handled = true;
        }
    }

    private void OnCopyKeyClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        if (sender is Button btn && btn.Tag is Guid keyId)
            try
            {
                var plainKey = ViewModel.GetDecryptedKey(keyId);
                var dataPackage = new DataPackage();
                dataPackage.SetText(plainKey);
                Clipboard.SetContent(dataPackage);
            }
            catch
            {
                // Silently fail
            }
    }

    private void OnApiKeyEditKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && sender is FrameworkElement element &&
            element.DataContext is KeyWrapper wrapper)
        {
            wrapper.CancelAddCommand?.Execute(null);
            e.Handled = true;
        }
    }
}
