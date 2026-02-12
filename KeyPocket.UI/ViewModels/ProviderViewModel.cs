using System;
using System.Collections.ObjectModel;
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
    [ObservableProperty] public partial string? BaseUrl { get; set; }

    // Manual implementation to avoid ObjectDisposedException during comparison
    private ImageSource? _customIconSource;

    [ObservableProperty] public partial string? Description { get; set; }

    [ObservableProperty] public partial string Icon { get; set; } = "\uE774"; // Globe

    [ObservableProperty] public partial bool IsCustomIcon { get; set; }

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial Brush StatusColor { get; set; } = new SolidColorBrush(Colors.Gray);

    [ObservableProperty] public partial string Type { get; set; } = string.Empty;

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
        var favoriteModels = provider.Models.Where(m => m.Tags.Contains(ModelTags.Favorite)).ToList();
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
        if (ProviderIconHelper.HasCustomIcon(iconPath))
        {
            var isDark = ThemeHelper.IsDarkTheme();
            var uri = ProviderIconHelper.GetIconUri(iconPath, isDark);

            if (uri != null)
            {
                CustomIconSource = new BitmapImage(uri);
                IsCustomIcon = true;
                return;
            }
        }

        // 2. Fallback to default icon
        IsCustomIcon = false;
        CustomIconSource = null;
        Icon = ProviderIconHelper.DefaultIconGlyph;
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

}
