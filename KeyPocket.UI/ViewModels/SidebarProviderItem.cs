using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KeyPocket.UI.ViewModels;

/// <summary>
///     侧边栏服务商项的 ViewModel
/// </summary>
public partial class SidebarProviderItem : ObservableObject
{
    [ObservableProperty] private string? _iconPath;

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty] private string _type = string.Empty;

    public Guid Id { get; set; }
}