using System;
using System.Collections.Generic;
using System.IO;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;
using Windows.Storage;

namespace KeyPocket.UI.Helpers;

/// <summary>
///     服务商预设图标管理辅助类
/// </summary>
public static class ProviderIconHelper
{
    /// <summary>
    ///     默认图标字形（固定为 Robot）
    /// </summary>
    public const string DefaultIconGlyph = "\uE99A";

    /// <summary>
    ///     检查是否为自定义图标（即 IconPath 不为空）
    /// </summary>
    /// <param name="iconPath">图标路径</param>
    /// <returns>是否为自定义图标</returns>
    public static bool HasCustomIcon(string? iconPath)
    {
        return !string.IsNullOrEmpty(iconPath);
    }

    /// <summary>
    ///     获取图标的 URI（无论是预设还是自定义路径）
    /// </summary>
    /// <param name="iconPath">图标路径</param>
    /// <param name="isDarkTheme">是否为暗色主题</param>
    /// <returns>Uri 或 null（如果路径无效）</returns>
    public static Uri? GetIconUri(string? iconPath, bool isDarkTheme)
    {
        if (string.IsNullOrEmpty(iconPath)) return null;

        // 1. 预设名称
        if (IsPresetName(iconPath)) return GetPresetIconUri(iconPath, isDarkTheme);

        // 2. 自定义路径
        try
        {
            // 尝试直接作为 URI 处理 (URL 或 ms-appx)
            if (Uri.TryCreate(iconPath, UriKind.Absolute, out var uri)) return uri;

            // 尝试作为文件路径处理
            return new Uri(iconPath);
        }
        catch
        {
            // 尝试作为相对路径（fallback logic from SearchViewModel context）
            try
            {
                return new Uri($"ms-appx:///{iconPath.TrimStart('/')}");
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    ///     根据路径和主题解析图标元素
    /// </summary>
    /// <param name="iconPath">图标路径或预设名称</param>
    /// <param name="isDarkTheme">是否为暗色主题</param>
    /// <returns>IconElement (FontIcon 或 ImageIcon)</returns>
    public static IconElement ResolveIconElement(string? iconPath, bool isDarkTheme)
    {
        if (!HasCustomIcon(iconPath)) return new FontIcon { Glyph = DefaultIconGlyph };

        // 判断是否为预设名称
        if (IsPresetName(iconPath))
        {
            var uri = GetPresetIconUri(iconPath, isDarkTheme);
            return new ImageIcon { Source = new BitmapImage(uri) };
        }

        // 自定义文件路径
        try
        {
            return new ImageIcon { Source = new BitmapImage(new Uri(iconPath)) };
        }
        catch
        {
            return new FontIcon { Glyph = DefaultIconGlyph };
        }
    }

    /// <summary>
    ///     将预设图标名称转换为带主题的 URI
    /// </summary>
    /// <param name="presetName">预设名称</param>
    /// <param name="isDarkTheme">是否为暗色主题</param>
    /// <returns>图标 URI</returns>
    public static Uri GetPresetIconUri(string presetName, bool isDarkTheme)
    {
        var baseName = presetName.ToLower();
        var suffix = isDarkTheme ? "-dark" : "-light";

        // 检查主题特定图标是否存在
        try
        {
            var appInstalledPath = Package.Current.InstalledLocation.Path;
            var assetsPath = Path.Combine(appInstalledPath, "Assets", "ProviderIcons");
            var themeSpecificPath = Path.Combine(assetsPath, $"{baseName}{suffix}.png");

            if (File.Exists(themeSpecificPath))
                return new Uri($"ms-appx:///Assets/ProviderIcons/{baseName}{suffix}.png");
        }
        catch
        {
            // Ignore file access errors
        }

        // 回退到基础图标
        return new Uri($"ms-appx:///Assets/ProviderIcons/{baseName}.png");
    }

    /// <summary>
    ///     获取所有可用的预设图标名称列表
    /// </summary>
    /// <returns>预设图标名称列表</returns>
    public static string[] GetAllPresetIconNames()
    {
        var appInstalledPath = Package.Current.InstalledLocation.Path;
        var assetsPath = Path.Combine(appInstalledPath, "Assets", "ProviderIcons");

        if (!Directory.Exists(assetsPath)) return Array.Empty<string>();

        var files = Directory.GetFiles(assetsPath, "*.png");
        var names = new HashSet<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            // Remove -dark or -light suffix (case-insensitive)
            if (fileName.EndsWith("-dark", StringComparison.OrdinalIgnoreCase) || 
                fileName.EndsWith("-light", StringComparison.OrdinalIgnoreCase))
            {
                var lastDashIndex = fileName.LastIndexOf('-');
                if (lastDashIndex > 0)
                {
                    fileName = fileName.Substring(0, lastDashIndex).Trim();
                }
            }
            names.Add(fileName);
        }

        var result = new string[names.Count];
        names.CopyTo(result);
        return result;
    }

    /// <summary>
    ///     判断路径是否为预设图标名称（而非文件路径）
    /// </summary>
    /// <param name="iconPath">图标路径</param>
    /// <returns>是否为预设名称</returns>
    public static bool IsPresetName(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath)) return false;

        // Allow dots in preset names (e.g. z.ai) but ensure it is not a path
        return !(iconPath.Contains('/') || iconPath.Contains('\\'));
    }
    /// <summary>
    ///     Model Icon Mapping configuration class
    /// </summary>
    private class ModelIconMapping
    {
        public List<string> Keywords { get; set; } = new();
        public string Icon { get; set; } = string.Empty;
    }

    private static List<ModelIconMapping> _modelIconMappings = new();
    private static bool _isMappingLoaded = false;

    /// <summary>
    ///     Load model icon mapping from AppData or Defaults
    /// </summary>
    private static void EnsureMappingLoaded()
    {
        if (_isMappingLoaded) return;

        try
        {
            string jsonContent = null;
            var localFolder = ApplicationData.Current.LocalFolder;
            var mappingFile = Path.Combine(localFolder.Path, "model_icon_mapping.json");

            // 1. Try load from LocalFolder (User Override)
            if (File.Exists(mappingFile))
            {
                jsonContent = File.ReadAllText(mappingFile);
            }
            else
            {
                // 2. Load from Assets/Defaults (Built-in)
                var appInstalledPath = Package.Current.InstalledLocation.Path;
                var defaultFile = Path.Combine(appInstalledPath, "Assets", "Defaults", "model_icon_mapping.json");
                
                if (File.Exists(defaultFile))
                {
                    jsonContent = File.ReadAllText(defaultFile);
                    
                    // Optional: Copy to LocalFolder for user customization
                    try { File.Copy(defaultFile, mappingFile); } catch { }
                }
            }

            if (!string.IsNullOrEmpty(jsonContent))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                // Deserialize as Dictionary<IconName, List<Keywords>>
                var rawMappings = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonContent, options);
                
                if (rawMappings != null)
                {
                    _modelIconMappings = new List<ModelIconMapping>();
                    foreach (var kvp in rawMappings)
                    {
                        _modelIconMappings.Add(new ModelIconMapping
                        {
                            Icon = kvp.Key,
                            Keywords = kvp.Value
                        });
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fail silently or log
        }
        finally
        {
            _isMappingLoaded = true;
        }
    }

    /// <summary>
    ///     Get icon name for a specific model ID/Name based on mapping rules
    /// </summary>
    public static string? GetIconForModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return null;
        
        EnsureMappingLoaded();

        var target = modelId.ToLowerInvariant();

        foreach (var mapping in _modelIconMappings)
        {
            // Case-insensitive check
            // "gpt" matches "gpt-4", "chatgpt", etc? 
            // User requirement: "-" belongs to match content, "dall-e" matches "dall-e" but not "dall" if "dall" is not in keywords?
            // User said: "dall-e" as whole match instead of split "dall" and "e".
            // Implementation: Simple Contains check handles "dall-e" correctly. 
            // If keywords=["gpt"], "gpt-4" contains "gpt".
            // If keywords=["dall-e"], "dall-e-3" contains "dall-e".
            
            foreach (var keyword in mapping.Keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;
                
                if (target.Contains(keyword.ToLowerInvariant()))
                {
                    return mapping.Icon;
                }
            }
        }

        return null;
    }
}