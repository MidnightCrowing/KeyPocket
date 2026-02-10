using System.Threading.Tasks;
using KeyPocket.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Pages.ProviderSettings;

public class ProviderSettingsSectionBase : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ProviderSettingsViewModel),
            typeof(ProviderSettingsSectionBase),
            new PropertyMetadata(null));

    public ProviderSettingsViewModel? ViewModel
    {
        get => (ProviderSettingsViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public virtual Task SaveAsync()
    {
        return Task.CompletedTask;
    }
}
