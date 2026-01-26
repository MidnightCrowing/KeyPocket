using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using KeyPocket.UI.Controls;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI.Pages;

public sealed partial class KeysPage : Page
{
    public KeysPage()
    {
        InitializeComponent();
        ViewModel = new KeysViewModel(App.ProviderService);
        DataContext = ViewModel;
    }

    public KeysViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.LoadData();
        base.OnNavigatedTo(e);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CopyButton button && button.Tag is Guid keyId)
        {
            // Find the key and decrypt it
            var keyItem = ViewModel.FilteredKeys.FirstOrDefault(k => k.Id == keyId);
            if (keyItem != null)
            {
                var decryptedKey = App.ProviderService.GetDecryptedApiKey(keyItem.ProviderId, keyId);
                if (!string.IsNullOrEmpty(decryptedKey))
                {
                    var package = new DataPackage();
                    package.SetText(decryptedKey);
                    Clipboard.SetContent(package);
                }
            }
        }
    }
}