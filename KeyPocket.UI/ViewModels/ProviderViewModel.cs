using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using KeyPocket.Core.Models;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using Microsoft.UI.Xaml;


namespace KeyPocket.UI.ViewModels;

public partial class ProviderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    public Guid Id { get; private set; }

    [ObservableProperty]
    private string _icon = "\uE774"; // Globe

    [ObservableProperty]
    private string? _description;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string? _baseUrl;

    public bool HasBaseUrl => !string.IsNullOrEmpty(BaseUrl);

    /// <summary>
    /// 获取 API Mode 的颜色（根据当前主题）
    /// </summary>
    public string ApiModeColor => Type switch
    {
        "OpenAI API" => ThemeHelper.IsDarkTheme() ? "#1A7F64" : "#10A37F",
        "Claude API" => ThemeHelper.IsDarkTheme() ? "#B85C3A" : "#D97757",
        "Google Gemini API" => ThemeHelper.IsDarkTheme() ? "#5E97F6" : "#4285F4",
        _ => ThemeHelper.IsDarkTheme() ? "#9CA3AF" : "#6B7280"
    };

    [ObservableProperty]
    private Brush _statusColor = new SolidColorBrush(Colors.Gray);

    public ObservableCollection<ModelItem> Models { get; set; } = new();
    public ObservableCollection<KeyItem> Keys { get; set; } = new();
    
    // Core Model Reference (Optional, but good for linking back)
    public Provider? CoreModel { get; private set; }

    public ProviderViewModel() { }

    public ProviderViewModel(Provider provider, ProviderService? providerService = null)
    {
        CoreModel = provider;
        Id = provider.Id;
        Name = provider.Name;
        Description = provider.Description;
        Type = provider.Type;
        BaseUrl = provider.ApiBaseUrl;
        UpdateIcon(provider.Type, provider.IconPath);
        
        // Populate favorite models
        var favoriteModels = provider.Models.Where(m => m.IsFavorite).ToList();
        foreach (var model in favoriteModels)
        {
            Models.Add(new ModelItem 
            { 
                Id = model.Id, 
                Name = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Id : model.DisplayName 
            });
        }
        
        // Populate favorite API keys
        var favoriteKeys = provider.ApiKeys.Where(k => k.IsFavorite).ToList();
        foreach (var key in favoriteKeys)
        {
            // Decrypt the key for display
            string decryptedKey = string.Empty;
            if (providerService != null)
            {
                decryptedKey = providerService.GetDecryptedApiKey(provider.Id, key.Id);
            }
            
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

    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.ImageSource? _customIconSource;

    [ObservableProperty]
    private bool _isCustomIcon;

    public void UpdateIcon(string type, string? iconPath = null)
    {
        // 1. Try Custom Icon
        if (!string.IsNullOrEmpty(iconPath))
        {
            try
            {
                var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var fullPath = System.IO.Path.Combine(localFolder.Path, iconPath);
                
                if (System.IO.File.Exists(fullPath))
                {
                    CustomIconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(fullPath));
                    IsCustomIcon = true;
                    return;
                }
            }
            catch { /* Ignore load errors, fallback to glyph */ }
        }

        // 2. Fallback to Glyph
        IsCustomIcon = false;
        CustomIconSource = null;
        Icon = "\uE99A"; // Default icon for all types
    }
}

// Keep these simple wrapper/items for now, or extract fully later
public class ModelItem { public string Name { get; set; } = ""; public string Id { get; set; } = ""; }
public class KeyItem 
{ 
    public string KeyStart { get; set; } = "";
    public string KeyEnd { get; set; } = "";
    public string FullKey { get; set; } = "";
    public string? Tag { get; set; }
    
    public bool HasTag => !string.IsNullOrWhiteSpace(Tag);
    
    /// <summary>
    /// 根据 Tag 内容返回对应的颜色（支持主题适配）
    /// </summary>
    public string TagColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Tag)) return "#6B7280"; // 默认灰色
            
            var tagLower = Tag.ToLower();
            var isDark = KeyPocket.UI.Helpers.ThemeHelper.IsDarkTheme();
            
            // 开发/测试相关
            if (tagLower.Contains("dev") || tagLower.Contains("开发") || tagLower.Contains("test") || tagLower.Contains("测试"))
                return isDark ? "#60A5FA" : "#3B82F6"; // 蓝色
            
            // 生产/正式相关
            if (tagLower.Contains("prod") || tagLower.Contains("生产") || tagLower.Contains("正式") || tagLower.Contains("production"))
                return isDark ? "#34D399" : "#10B981"; // 绿色
            
            // 免费相关
            if (tagLower.Contains("free") || tagLower.Contains("免费") || tagLower.Contains("trial") || tagLower.Contains("试用"))
                return isDark ? "#A78BFA" : "#8B5CF6"; // 紫色
            
            // 收费/付费相关
            if (tagLower.Contains("paid") || tagLower.Contains("收费") || tagLower.Contains("付费") || tagLower.Contains("premium"))
                return isDark ? "#FBBF24" : "#F59E0B"; // 黄色
            
            // 临时/暂存相关
            if (tagLower.Contains("temp") || tagLower.Contains("临时") || tagLower.Contains("暂存") || tagLower.Contains("staging"))
                return isDark ? "#FB923C" : "#F97316"; // 橙色
            
            // 备份相关
            if (tagLower.Contains("backup") || tagLower.Contains("备份") || tagLower.Contains("bak"))
                return isDark ? "#94A3B8" : "#64748B"; // 石板灰
            
            // 默认颜色
            return isDark ? "#9CA3AF" : "#6B7280"; // 灰色
        }
    }
}
