using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Dialogs;

public sealed partial class AddProviderDialog : ContentDialog
{
    public AddProviderDialog()
    {
        this.InitializeComponent();
        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    public string ProviderName => NameBox.Text;
    public string ProviderType => (TypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Custom";
    public string BaseUrl => UrlBox.Text;
    public string Description => DescBox.Text;

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Simple validation
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            args.Cancel = true;
            NameBox.Header = "Name (Required!)"; 
        }
    }
}
