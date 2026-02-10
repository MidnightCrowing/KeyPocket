using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using KeyPocket.UI.Helpers;

namespace KeyPocket.UI.ViewModels;

/// <summary>
///     服务商分组,用于 KeysPage HeaderedTreeView 的分组展示
/// </summary>
public partial class KeyProviderGroupViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomIcon))]
    [NotifyPropertyChangedFor(nameof(DefaultGlyph))]
    [NotifyPropertyChangedFor(nameof(IconUri))]
    private string? _providerIcon;

    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;

    public List<KeyItemViewModel> Keys { get; set; } = new();

    public bool IsCustomIcon => ProviderIconHelper.HasCustomIcon(ProviderIcon);
    public string DefaultGlyph => ProviderIconHelper.DefaultIconGlyph;
    public Uri? IconUri => ProviderIconHelper.GetIconUri(ProviderIcon, ThemeHelper.IsDarkTheme());

    public void RefreshIcon()
    {
        OnPropertyChanged(nameof(IconUri));
    }
}