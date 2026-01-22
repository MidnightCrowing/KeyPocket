using CommunityToolkit.Mvvm.ComponentModel;
using System;


namespace KeyPocket.UI.ViewModels;

/// <summary>
/// 侧边栏服务商项的 ViewModel
/// </summary>
public partial class SidebarProviderItem : ObservableObject
{
    public Guid Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _iconPath;

    [ObservableProperty]
    private string _type = string.Empty;

    /// <summary>
    /// 是否有自定义图标
    /// </summary>
    public bool HasCustomIcon => !string.IsNullOrEmpty(IconPath);

    /// <summary>
    /// 默认图标字形（固定为 Robot）
    /// </summary>
    public string DefaultIconGlyph => "\uE99A"; // 统一使用 Robot 图标
}
