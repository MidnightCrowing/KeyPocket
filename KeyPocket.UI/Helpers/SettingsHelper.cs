using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Windows.Storage;
using Microsoft.UI.Xaml;

namespace KeyPocket.UI.Helpers;

public abstract class ObservableSettings : INotifyPropertyChanged
{
    private static readonly string ConfigFilePath = GetConfigPath();
    private readonly object _fileLock = new();
    private readonly ConcurrentDictionary<string, object?> _settings = new();

    protected ObservableSettings()
    {
        Load();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string GetConfigPath()
    {
        string path;
        try
        {
            // 对于 WinUI 3 打包应用，使用 LocalFolder (LocalState 目录)
            path = ApplicationData.Current.LocalFolder.Path;
        }
        catch (Exception ex)
        {
            // 记录路径获取失败的原因
            var fallbackBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KeyPocket");
            if (!Directory.Exists(fallbackBase)) Directory.CreateDirectory(fallbackBase);
            File.AppendAllText(Path.Combine(fallbackBase, "PathError.txt"),
                $"[{DateTime.Now}] GetConfigPath failed: {ex.Message}\n{ex.StackTrace}\n");
            path = fallbackBase;
        }

        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        return Path.Combine(path, "AppConfig.json");
    }

    protected T GetOrCreateDefault<T>(T defaultValue, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName == null) return defaultValue;

        if (_settings.TryGetValue(propertyName, out var value))
        {
            if (value is JsonElement element)
            {
                var deserialized = element.Deserialize<T>();
                _settings[propertyName] = deserialized;
                return deserialized ?? defaultValue;
            }

            return (T)value!;
        }

        _settings[propertyName] = defaultValue;
        return defaultValue;
    }

    protected void Set<T>(T value, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName == null) return;

        _settings[propertyName] = value;
        OnPropertyChanged(propertyName);
        Save();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
                lock (_fileLock)
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                    if (data != null)
                        foreach (var kv in data)
                            _settings[kv.Key] = kv.Value;
                }
        }
        catch (Exception ex)
        {
            LogException(ex, "Load");
        }
    }

    private void Save()
    {
        try
        {
            lock (_fileLock)
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
        }
        catch (Exception ex)
        {
            LogException(ex, "Save");
        }
    }

    private void LogException(Exception ex, string operation)
    {
        try
        {
            var logPath = Path.Combine(Path.GetDirectoryName(ConfigFilePath) ?? "", "ErrorLog.txt");
            var message =
                $"[{DateTime.Now}] Operation: {operation}\nException: {ex.Message}\nStackTrace: {ex.StackTrace}\n\n";
            File.AppendAllText(logPath, message);
        }
        catch
        {
            /* 最后的防线 */
        }
    }
}

public partial class SettingsHelper : ObservableSettings
{
    private static readonly Lazy<SettingsHelper> Instance = new(() => new SettingsHelper());

    private SettingsHelper()
    {
    }

    public static SettingsHelper Current => Instance.Value;

    public ElementTheme SelectedAppTheme
    {
        get => GetOrCreateDefault(ElementTheme.Dark);
        set => Set(value);
    }

    // 货币设置: 默认 USD，可切换为 CNY 等
    public string SelectedCurrency
    {
        get => GetOrCreateDefault("USD");
        set => Set(value);
    }

    // 可用货币列表
    public List<string> AvailableCurrencies
    {
        get => GetOrCreateDefault(new List<string> { "USD", "CNY" });
        set => Set(value);
    }

    // 汇率字典: Key="SOURCE_TARGET" (e.g. "USD_CNY"), Value=Rate
    public Dictionary<string, decimal> ExchangeRates
    {
        get => GetOrCreateDefault(new Dictionary<string, decimal>
        {
            { "USD_CNY", 7.0m }
        });
        set => Set(value);
    }

    // Currency Symbols: Key="CODE" (e.g. "USD"), Value="SYMBOL" (e.g. "$")
    public Dictionary<string, string> CurrencySymbols
    {
        get => GetOrCreateDefault(new Dictionary<string, string>
        {
            { "USD", "$" },
            { "CNY", "¥" }
        });
        set => Set(value);
    }
}