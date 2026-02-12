using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KeyPocket.UI.ViewModels;

/// <summary>
///     侧边栏服务商项的 ViewModel
/// </summary>
public partial class SidebarProviderItem : ObservableObject
{
    [ObservableProperty] public partial string? IconPath { get; set; }

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;

    [ObservableProperty] public partial string Type { get; set; } = string.Empty;

    public Guid Id { get; set; }
}
