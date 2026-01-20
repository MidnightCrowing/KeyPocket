using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Dialogs;

public sealed partial class AddKeyDialog : ContentDialog
{
    public string KeyName => KeyNameBox.Text;
    public string KeySecret => KeySecretBox.Password;

    public AddKeyDialog()
    {
        this.InitializeComponent();
    }
}
