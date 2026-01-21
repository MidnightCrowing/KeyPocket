using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using System;
using System.Linq;

namespace KeyPocket.UI.Pages;

public sealed partial class KeysPage : Page
{
    public KeysViewModel ViewModel { get; private set; }

    public KeysPage()
    {
        this.InitializeComponent();
        ViewModel = new KeysViewModel(App.ProviderService);
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.LoadData();
        base.OnNavigatedTo(e);
    }

    private void CopyButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Controls.CopyButton button && button.Tag is Guid keyId)
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

