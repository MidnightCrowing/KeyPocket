using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using KeyPocket.UI.Helpers;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KeyPocket.UI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            
            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogException("App_UnhandledException", e.Exception);
            // Optional: e.Handled = true; if we want to try to suppress crash
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            LogException("CurrentDomain_UnhandledException", e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException("TaskScheduler_UnobservedTaskException", e.Exception);
            e.SetObserved(); 
        }

        private void LogException(string source, Exception? ex)
        {
            if (ex == null) return;

            try
            {
                var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var filePath = System.IO.Path.Combine(folder.Path, "crash.log");
                
                string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\nData: {ex.Data}\nMessage: {ex.Message}\nStack: {ex.StackTrace}\n\n";

                // Use synchronous write as we might be crashing
                File.AppendAllText(filePath, logContent, System.Text.Encoding.UTF8);
            }
            catch (Exception)
            {
                // Last ditch effort: suppress so we don't throw inside an exception handler
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
                InitializeServices();



            _window = new MainWindow();
            MainWindow = _window;

            // Make the window available to helpers first so Initialize can apply the theme
            WindowHelper.SetMainWindow(_window);

            // Initialize helpers before showing the window
            ThemeHelper.Initialize();

            _window.Activate();
        }

        public static Window MainWindow { get; private set; } = null!;
        public static KeyPocket.Core.Services.ProviderService ProviderService { get; private set; } = null!;

        private void InitializeServices()
        {
            var storagePath = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "data.json");
            var storage = new KeyPocket.Core.Storage.JsonFileStorageProvider(storagePath);
            var protector = new KeyPocket.Core.Crypto.DpapiSecretProtector();
            ProviderService = new KeyPocket.Core.Services.ProviderService(storage, protector);
        }
    }
}