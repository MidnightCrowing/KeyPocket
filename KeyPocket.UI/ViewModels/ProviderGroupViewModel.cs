using System;
using System.Collections.Generic;
using KeyPocket.UI.Helpers;

namespace KeyPocket.UI.ViewModels;

/// <summary>
///     服务商分组,用于 HeaderedTreeView 的分组展示
/// </summary>
public class ProviderGroupViewModel
{
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderIcon { get; set; }
    public List<ModelItemViewModel> Models { get; set; } = new();

    public bool IsCustomIcon => ProviderIconHelper.HasCustomIcon(ProviderIcon);
    public string DefaultGlyph => ProviderIconHelper.DefaultIconGlyph;
    public Uri? IconUri => ProviderIconHelper.GetIconUri(ProviderIcon, ThemeHelper.IsDarkTheme());
}