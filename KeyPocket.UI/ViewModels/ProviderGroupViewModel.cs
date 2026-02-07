using System.Collections.Generic;

namespace KeyPocket.UI.ViewModels;

/// <summary>
/// 服务商分组,用于 HeaderedTreeView 的分组展示
/// </summary>
public class ProviderGroupViewModel
{
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderIcon { get; set; }
    public List<ModelItemViewModel> Models { get; set; } = new();
}
