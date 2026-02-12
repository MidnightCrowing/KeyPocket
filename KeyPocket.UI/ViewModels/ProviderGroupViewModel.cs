using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.UI.Helpers;

namespace KeyPocket.UI.ViewModels;

/// <summary>
///     服务商分组,用于 HeaderedTreeView 的分组展示
/// </summary>
public partial class ProviderGroupViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomIcon))]
    [NotifyPropertyChangedFor(nameof(DefaultGlyph))]
    [NotifyPropertyChangedFor(nameof(IconUri))]
    public partial string? ProviderIcon { get; set; }

    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;

    public List<ModelItemViewModel> Models { get; set; } = new();

    public bool IsCustomIcon => ProviderIconHelper.HasCustomIcon(ProviderIcon);
    public string DefaultGlyph => ProviderIconHelper.DefaultIconGlyph;
    public Uri? IconUri => ProviderIconHelper.GetIconUri(ProviderIcon, ThemeHelper.IsDarkTheme());
}
