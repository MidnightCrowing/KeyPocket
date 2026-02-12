using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Models;

namespace KeyPocket.UI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ProviderService _providerService;

    [ObservableProperty] public partial ObservableCollection<SearchResultItem> SearchResults { get; set; } = new();

    public SearchViewModel(ProviderService providerService)
    {
        _providerService = providerService;
    }

    public void PerformSearch(string query)
    {
        SearchResults.Clear();

        if (string.IsNullOrWhiteSpace(query)) return;

        var lowerQuery = query.ToLower();
        var isDark = ThemeHelper.IsDarkTheme();

        // 1. 搜索服务商
        var providers = _providerService.GetAllProviders()
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(p =>
            {
                var item = new SearchResultItem
                {
                    Title = p.Name,
                    Description = p.Type,
                    Type = SearchResultType.Provider,
                    Data = p.Id
                };

                // 设置图标
                if (ProviderIconHelper.HasCustomIcon(p.IconPath))
                {
                    var uri = ProviderIconHelper.GetIconUri(p.IconPath, isDark);
                    if (uri != null)
                    {
                        item.IconKind = IconType.ImagePath;
                        item.IconPath = uri;
                    }
                    else
                    {
                        item.IconKind = IconType.Glyph;
                        item.Icon = ProviderIconHelper.DefaultIconGlyph;
                    }
                }
                else
                {
                    // 使用默认图标
                    item.IconKind = IconType.Glyph;
                    item.Icon = ProviderIconHelper.DefaultIconGlyph;
                }

                return item;
            });

        foreach (var provider in providers) SearchResults.Add(provider);

        // 2. 搜索模型
        var models = _providerService.GetAllProviders()
            .SelectMany(p => p.Models.Select(m => new { Model = m, ProviderName = p.Name }))
            .Where(x => x.Model.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        (x.Model.DisplayName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(5);

        foreach (var x in models)
        {
            var item = new SearchResultItem
            {
                Title = x.Model.DisplayName ?? x.Model.Id,
                Description = $"Model · {x.ProviderName}",
                Type = SearchResultType.Model,
                Data = x.Model.Id,
                Icon = "\uF158", // Fallback AI icon
                IconKind = IconType.Glyph
            };

            // Try to resolve model icon
            var iconName = ProviderIconHelper.GetIconForModel(x.Model.Id);
            if (iconName == null && !string.IsNullOrEmpty(x.Model.DisplayName))
                iconName = ProviderIconHelper.GetIconForModel(x.Model.DisplayName);

            if (!string.IsNullOrEmpty(iconName))
            {
                var uri = ProviderIconHelper.GetPresetIconUri(iconName, isDark);
                if (uri != null)
                {
                    item.IconPath = uri;
                    item.IconKind = IconType.ImagePath;
                }
            }

            SearchResults.Add(item);

        }
        // 3. Search API Key tags
        var tags = _providerService.GetAllProviders()
            .SelectMany(p => p.ApiKeys)
            .Select(k => k.Tag)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .Where(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(5);

        foreach (var tag in tags)
            SearchResults.Add(new SearchResultItem
            {
                Title = tag,
                Description = "API Key Tag",
                Type = SearchResultType.KeyTag,
                Data = tag,
                Icon = "\uE8EC", // Tag icon
                IconKind = IconType.Glyph
            });

        // 4. Special keyword: crash.log
        if (lowerQuery.Contains("crash") || lowerQuery.Contains("log"))
            SearchResults.Add(new SearchResultItem
            {
                Title = "crash.log",
                Description = "Application log file",
                Type = SearchResultType.SystemFile,
                Data = CrashLogHelper.GetCrashLogPath(),
                Icon = "\uE7C3" // Page icon
            });

        // 5. 特殊关键词：model_icon_mapping.json
        if (lowerQuery.Contains("icon") || lowerQuery.Contains("mapping") || lowerQuery.Contains("json"))
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var mappingFile = Path.Combine(localFolder.Path, "model_icon_mapping.json");

            // Ensure file exists (ProviderIconHelper handles copying if needed, but let's be safe)
            if (File.Exists(mappingFile))
                SearchResults.Add(new SearchResultItem
                {
                    Title = "model_icon_mapping.json",
                    Description = "Model Icon Mapping Configuration",
                    Type = SearchResultType.SystemFile,
                    Data = mappingFile,
                    Icon = "\uE7C3" // Page icon
                });
        }
    }
}
