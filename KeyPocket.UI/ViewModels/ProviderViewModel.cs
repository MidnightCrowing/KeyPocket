using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace KeyPocket.UI.ViewModels;

public partial class ProviderViewModel : ObservableObject
{
    [ObservableProperty] private string? _baseUrl;

    // Manual implementation to avoid ObjectDisposedException during comparison
    private ImageSource? _customIconSource;

    [ObservableProperty] private string? _description;

    [ObservableProperty] private string _icon = "\uE774"; // Globe

    [ObservableProperty] private bool _isCustomIcon;

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty] private Brush _statusColor = new SolidColorBrush(Colors.Gray);

    [ObservableProperty] private string _type = string.Empty;

    public ProviderViewModel()
    {
    }

    public ProviderViewModel(Provider provider, ProviderService? providerService = null)
    {
        CoreModel = provider;
        Id = provider.Id;
        Name = provider.Name;
        Description = provider.Description;
        Type = provider.Type;
        BaseUrl = provider.ApiBaseUrl;
        SortOrder = provider.SortOrder;
        UpdateIcon(provider.Type, provider.IconPath);

        // Populate favorite models
        var favoriteModels = provider.Models.Where(m => m.IsFavorite).ToList();
        foreach (var model in favoriteModels)
            Models.Add(new ModelItem
            {
                Id = model.Id,
                Name = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Id : model.DisplayName
            });

        // Populate favorite API keys
        var favoriteKeys = provider.ApiKeys.Where(k => k.IsFavorite).ToList();
        foreach (var key in favoriteKeys)
        {
            // Decrypt the key for display
            var decryptedKey = string.Empty;
            if (providerService != null) decryptedKey = providerService.GetDecryptedApiKey(provider.Id, key.Id);

            if (!string.IsNullOrEmpty(decryptedKey))
            {
                var keyStart = decryptedKey.Length >= 7 ? decryptedKey.Substring(0, 7) : decryptedKey;
                var keyEnd = decryptedKey.Length >= 4 ? decryptedKey.Substring(decryptedKey.Length - 4) : "";

                Keys.Add(new KeyItem
                {
                    KeyStart = keyStart,
                    KeyEnd = keyEnd,
                    FullKey = decryptedKey,
                    Tag = key.Tag
                });
            }
        }
    }

    public ImageSource? CustomIconSource
    {
        get => _customIconSource;
        set
        {
            // Completely bypass equality check to avoid accessing properties of potentially disposed objects
            _customIconSource = value;
            OnPropertyChanged();
        }
    }

    public Guid Id { get; private set; }

    public int SortOrder { get; set; }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool HasBaseUrl => !string.IsNullOrEmpty(BaseUrl);

    /// <summary>
    ///     获取 API Mode 的颜色（根据当前主题）
    /// </summary>
    public string ApiModeColor => Type switch
    {
        "OpenAI API" => ThemeHelper.IsDarkTheme() ? "#1A7F64" : "#10A37F",
        "Claude API" => ThemeHelper.IsDarkTheme() ? "#B85C3A" : "#D97757",
        "Google Gemini API" => ThemeHelper.IsDarkTheme() ? "#5E97F6" : "#4285F4",
        _ => ThemeHelper.IsDarkTheme() ? "#9CA3AF" : "#6B7280"
    };

    public ObservableCollection<ModelItem> Models { get; set; } = new();
    public ObservableCollection<KeyItem> Keys { get; set; } = new();

    // Core Model Reference (Optional, but good for linking back)
    public Provider? CoreModel { get; }

    public void UpdateIcon(string type, string? iconPath = null)
    {
        // 1. Try resolving Icon
        if (!string.IsNullOrEmpty(iconPath))
        {
            // 判断是否为预设名称
            if (ProviderIconHelper.IsPresetName(iconPath))
            {
                // 预设名称（如 "openai"）
                var isDark = ThemeHelper.IsDarkTheme();
                var uri = ProviderIconHelper.GetPresetIconUri(iconPath, isDark);
                CustomIconSource = new BitmapImage(uri);
                IsCustomIcon = true;
                return;
            }

            // 自定义文件路径
            try
            {
                // Direct absolute path check
                if (File.Exists(iconPath))
                {
                    CustomIconSource = new BitmapImage(new Uri(iconPath));
                    IsCustomIcon = true;
                    return;
                }

                // Try as URI (e.g., web URL or appx URI not covered by File.Exists)
                CustomIconSource = new BitmapImage(new Uri(iconPath));
                IsCustomIcon = true;
                return;
            }
            catch
            {
                // Fall through to default
            }
        }

        // 2. Fallback to default icon based on type
        IsCustomIcon = false;
        CustomIconSource = null;
        Icon = "\uE99A"; // Default icon for all types
    }

    public void RefreshIcon()
    {
        // Re-run logic with current theme
        UpdateIcon(Type, CoreModel?.IconPath);
    }
}

// Keep these simple wrapper/items for now, or extract fully later
public class ModelItem
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
}

public class KeyItem
{
    public string KeyStart { get; set; } = "";
    public string KeyEnd { get; set; } = "";
    public string FullKey { get; set; } = "";
    public string? Tag { get; set; }

    public bool HasTag => !string.IsNullOrWhiteSpace(Tag);

    /// <summary>
    ///     根据 Tag 内容返回对应的颜色（支持主题适配）
    /// </summary>
    public string TagColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Tag)) return "#6B7280"; // 默认灰色

            var tagLower = Tag.ToLower();
            var isDark = ThemeHelper.IsDarkTheme();

            // 开发/测试相关
            if (tagLower.Contains("dev") || tagLower.Contains("开发") || tagLower.Contains("test") ||
                tagLower.Contains("测试"))
                return isDark ? "#60A5FA" : "#3B82F6"; // 蓝色

            // 生产/正式相关
            if (tagLower.Contains("prod") || tagLower.Contains("生产") || tagLower.Contains("正式") ||
                tagLower.Contains("production"))
                return isDark ? "#34D399" : "#10B981"; // 绿色

            // 免费相关
            if (tagLower.Contains("free") || tagLower.Contains("免费") || tagLower.Contains("trial") ||
                tagLower.Contains("试用"))
                return isDark ? "#A78BFA" : "#8B5CF6"; // 紫色

            // 收费/付费相关
            if (tagLower.Contains("paid") || tagLower.Contains("收费") || tagLower.Contains("付费") ||
                tagLower.Contains("premium"))
                return isDark ? "#FBBF24" : "#F59E0B"; // 黄色

            // 临时/暂存相关
            if (tagLower.Contains("temp") || tagLower.Contains("临时") || tagLower.Contains("暂存") ||
                tagLower.Contains("staging"))
                return isDark ? "#FB923C" : "#F97316"; // 橙色

            // 备份相关
            if (tagLower.Contains("backup") || tagLower.Contains("备份") || tagLower.Contains("bak"))
                return isDark ? "#94A3B8" : "#64748B"; // 石板灰

            // 默认颜色
            return isDark ? "#9CA3AF" : "#6B7280"; // 灰色
        }
    }
}