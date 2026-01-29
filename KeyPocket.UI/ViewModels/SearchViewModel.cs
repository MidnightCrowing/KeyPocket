using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.Core.Services;
using KeyPocket.UI.Helpers;
using KeyPocket.UI.Models;

namespace KeyPocket.UI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ProviderService _providerService;

    [ObservableProperty] private ObservableCollection<SearchResultItem> _searchResults = new();

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
                if (!string.IsNullOrEmpty(p.IconPath))
                {
                    // 有自定义图标路径
                    if (ProviderIconHelper.IsPresetName(p.IconPath))
                    {
                        // 预设名称（如 "openai"）
                        item.IconKind = IconType.ImagePath;
                        item.IconPath = ProviderIconHelper.GetPresetIconUri(p.IconPath, isDark);
                    }
                    else
                    {
                        // 自定义文件路径
                        item.IconKind = IconType.ImagePath;
                        var iconUri = p.IconPath.StartsWith("ms-appx://") || p.IconPath.StartsWith("http")
                            ? p.IconPath
                            : $"ms-appx:///{p.IconPath.TrimStart('/')}";
                        item.IconPath = new Uri(iconUri);
                    }
                }
                else
                {
                    // 使用默认图标
                    item.IconKind = IconType.Glyph;
                    item.Icon = "\uE99A"; // Provider icon
                }

                return item;
            });

        foreach (var provider in providers) SearchResults.Add(provider);

        // 2. 搜索模型
        var models = _providerService.GetAllProviders()
            .SelectMany(p => p.Models.Select(m => new { Model = m, ProviderName = p.Name }))
            .Where(x => x.Model.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        (x.Model.DisplayName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(5)
            .Select(x => new SearchResultItem
            {
                Title = x.Model.DisplayName ?? x.Model.Id,
                Description = $"Model · {x.ProviderName}",
                Type = SearchResultType.Model,
                Data = x.Model.Id,
                Icon = "\uF158" // AI icon
            });

        foreach (var model in models) SearchResults.Add(model);

        // 3. 特殊关键词：crash.log
        if (lowerQuery.Contains("crash") || lowerQuery.Contains("log"))
            SearchResults.Add(new SearchResultItem
            {
                Title = "crash.log",
                Description = "Application log file",
                Type = SearchResultType.SystemFile,
                Data = CrashLogHelper.GetCrashLogPath(),
                Icon = "\uE7C3" // Page icon
            });
    }
}