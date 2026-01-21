using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace KeyPocket.UI.Pages;

public sealed partial class ModelsPage : Page
{
    public ModelsViewModel ViewModel { get; private set; }

    public ModelsPage()
    {
        this.InitializeComponent();
        ViewModel = new ModelsViewModel(App.ProviderService);
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Reload data when navigated to ensure we have latest providers/models
        ViewModel.LoadData();
        base.OnNavigatedTo(e);
    }

    private void CopyButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Controls.CopyButton button && button.Tag is string text)
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
    }
}

