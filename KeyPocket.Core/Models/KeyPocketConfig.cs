using System.Collections.Generic;

namespace KeyPocket.Core.Models;

/// <summary>
/// 存储配置的根对象。
/// </summary>
public class KeyPocketConfig
{
    /// <summary>
    /// 配置版本。
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 所有供应商。
    /// </summary>
    public List<Provider> Providers { get; set; } = new();
}
