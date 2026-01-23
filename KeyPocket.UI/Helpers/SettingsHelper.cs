using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace KeyPocket.UI.Helpers;

public abstract class ObservableSettings : INotifyPropertyChanged
{
    private static readonly string ConfigFilePath = GetConfigPath();
    private readonly ConcurrentDictionary<string, object?> _settings = new();
    private readonly object _fileLock = new();

    private static string GetConfigPath()
    {
        string path;
        try
        {
            // 对于 WinUI 3 打包应用，使用 LocalFolder (LocalState 目录)
            path = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
        catch (Exception ex)
        {
            // 记录路径获取失败的原因
            var fallbackBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeyPocket");
            if (!Directory.Exists(fallbackBase)) Directory.CreateDirectory(fallbackBase);
            File.AppendAllText(Path.Combine(fallbackBase, "PathError.txt"), $"[{DateTime.Now}] GetConfigPath failed: {ex.Message}\n{ex.StackTrace}\n");
            path = fallbackBase;
        }
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
        return Path.Combine(path, "AppConfig.json");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected ObservableSettings()
    {
        Load();
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
            {
                lock (_fileLock)
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                    if (data != null)
                    {
                        foreach (var kv in data)
                        {
                            _settings[kv.Key] = kv.Value;
                        }
                    }
                }
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
            var message = $"[{DateTime.Now}] Operation: {operation}\nException: {ex.Message}\nStackTrace: {ex.StackTrace}\n\n";
            File.AppendAllText(logPath, message);
        }
        catch { /* 最后的防线 */ }
    }
}

public partial class SettingsHelper : ObservableSettings
{
    private static readonly Lazy<SettingsHelper> Instance = new(() => new SettingsHelper());
    public static SettingsHelper Current => Instance.Value;

    private SettingsHelper()
    {
    }

    public ElementTheme SelectedAppTheme
    {
        get => GetOrCreateDefault(ElementTheme.Dark);
        set => Set(value);
    }

    // 货币设置: 默认 USD，可切换为 CNY
    public string SelectedCurrency
    {
        get => GetOrCreateDefault("USD");
        set => Set(value);
    }

    // 美元到人民币的默认汇率（可调整）
    public decimal UsdToCnyRate
    {
        get => GetOrCreateDefault(7.0m);
        set => Set(value);
    }
}
