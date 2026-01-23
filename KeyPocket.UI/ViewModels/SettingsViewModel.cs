using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;
using System.Threading.Tasks;
using System.Diagnostics;


namespace KeyPocket.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _themeIndex;

    [ObservableProperty]
    private string _selectedCurrency;

    [ObservableProperty]
    private double _usdToCnyRate;

    [ObservableProperty]
    private bool _isRefreshing;

    public string Version
    {
        get
        {
            return ProcessInfoHelper.GetVersion() is Version version
                ? string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision)
                : string.Empty;
        }
    }

    public SettingsViewModel()
    {
        // 根据当前主题设置索引
        _themeIndex = ThemeHelper.Theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };

        // 初始化货币设置（从持久化设置读取）
        _selectedCurrency = SettingsHelper.Current.SelectedCurrency;
        _usdToCnyRate = (double)SettingsHelper.Current.UsdToCnyRate;
    }

    [RelayCommand]
    private async Task RefreshExchangeRateAsync()
    {
        // Ensure IsRefreshing is set and always cleared even on exception.
        try
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                IsRefreshing = true;
            });

            Debug.WriteLine("RefreshExchangeRateAsync: started");

            var (rate, error) = await ExchangeRateHelper.FetchUsdToCnyWithErrorAsync().ConfigureAwait(false);

            if (rate.HasValue)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    UsdToCnyRate = (double)rate.Value;
                });

                Debug.WriteLine($"RefreshExchangeRateAsync: succeeded rate={rate.Value}");
            }
            else
            {
                Debug.WriteLine($"RefreshExchangeRateAsync: failed with error={error}");

                // 在 UI 线程显示错误对话框
                await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var dlg = new ContentDialog
                    {
                        Title = "刷新汇率失败",
                        Content = string.IsNullOrWhiteSpace(error) ? "无法获取汇率，请检查网络或代理设置。" : error,
                        CloseButtonText = "确定"
                    };

                    _ = dlg.ShowAsync();
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RefreshExchangeRateAsync: exception {ex}");

            // 在 UI 线程显示错误对话框
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var dlg = new ContentDialog
                    {
                        Title = "刷新汇率异常",
                        Content = ex.Message,
                        CloseButtonText = "确定"
                    };

                    _ = dlg.ShowAsync();
                });
            }
            catch
            {
                // Swallow secondary exceptions from showing the dialog but keep the original logged.
            }
        }
        finally
        {
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    IsRefreshing = false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RefreshExchangeRateAsync: failed to clear IsRefreshing: {ex}");
            }
        }
    }

    partial void OnSelectedCurrencyChanged(string value)
    {
        SettingsHelper.Current.SelectedCurrency = value;
    }

    partial void OnUsdToCnyRateChanged(double value)
    {
        SettingsHelper.Current.UsdToCnyRate = (decimal)value;
    }

    partial void OnThemeIndexChanged(int value)
    {
        // 根据索引设置主题
        ThemeHelper.Theme = value switch
        {
            0 => ElementTheme.Light,
            1 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task OpenGitHubAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPocket"));
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task OpenFeedbackAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/MidnightCrowing/KeyPocket/issues"));
    }
}
