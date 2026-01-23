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
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
                InitializeServices();

                // 后台异步获取最新 USD->CNY 汇率（不阻塞启动）
                _ = System.Threading.Tasks.Task.Run(async () => await ExchangeRateHelper.FetchAndUpdateAsync().ConfigureAwait(false));

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