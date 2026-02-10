using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using KeyPocket.Core.Crypto;
using KeyPocket.Core.Services;
using KeyPocket.Core.Storage;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeyPocket.UI;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    public static Window MainWindow { get; private set; } = null!;
    public static ProviderService ProviderService { get; private set; } = null!;
    public static ModelFilterService ModelFilterService { get; private set; } = null!;

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        CrashLogHelper.LogException("App_UnhandledException", e.Exception);
        // Optional: e.Handled = true; if we want to try to suppress crash
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        CrashLogHelper.LogException("CurrentDomain_UnhandledException", e.ExceptionObject as Exception);
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        CrashLogHelper.LogException("TaskScheduler_UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        InitializeServices();

        _window = new MainWindow();
        MainWindow = _window;

        // Make the window available to helpers first so Initialize can apply the theme
        WindowHelper.SetMainWindow(_window);

        // Initialize helpers before showing the window
        ThemeHelper.Initialize();
        BackdropHelper.ApplyToMainWindow(SettingsHelper.Current.SelectedBackdrop);

        _window.Activate();
    }

    private void InitializeServices()
    {
        var storagePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "data.json");
        var storage = new JsonFileStorageProvider(storagePath);
        var protector = new DpapiSecretProtector();
        ProviderService = new ProviderService(storage, protector);
        ModelFilterService = new ModelFilterService();
    }
}
