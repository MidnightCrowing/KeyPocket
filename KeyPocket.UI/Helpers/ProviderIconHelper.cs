using System;
using System.Collections.Generic;
using System.IO;
using Windows.ApplicationModel;

namespace KeyPocket.UI.Helpers;

/// <summary>
///     服务商预设图标管理辅助类
/// </summary>
public static class ProviderIconHelper
{
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
        var appInstalledPath = Package.Current.InstalledLocation.Path;
        var assetsPath = Path.Combine(appInstalledPath, "Assets", "ProviderIcons");
        var themeSpecificPath = Path.Combine(assetsPath, $"{baseName}{suffix}.png");

        if (File.Exists(themeSpecificPath)) return new Uri($"ms-appx:///Assets/ProviderIcons/{baseName}{suffix}.png");

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
            // 移除 -dark 或 -light 后缀
            if (fileName.EndsWith("-dark") || fileName.EndsWith("-light"))
                fileName = fileName.Substring(0, fileName.LastIndexOf('-'));
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

        // 如果包含路径分隔符或文件扩展名，则为文件路径
        return !(iconPath.Contains('/') || iconPath.Contains('\\') || iconPath.Contains('.'));
    }
}