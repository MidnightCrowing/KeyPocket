using Windows.ApplicationModel.DataTransfer;
using KeyPocket.UI.Controls;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KeyPocket.UI.Pages;

public sealed partial class ModelsPage : Page
{
    public ModelsPage()
    {
        InitializeComponent();
        ViewModel = new ModelsViewModel(App.ProviderService);
        DataContext = ViewModel;
    }

    public ModelsViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Reload data when navigated to ensure we have latest providers/models
        ViewModel.LoadData();
        base.OnNavigatedTo(e);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CopyButton button && button.Tag is string text)
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
    }
}