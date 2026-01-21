using System;
using System.Collections.Generic;

namespace KeyPocket.Core.Models;

/// <summary>
/// API Key 模型。
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 关联的服务商 ID。
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// 加密后的 Key 字符串。
    /// </summary>
    public string EncryptedKey { get; set; } = string.Empty;

    /// <summary>
    /// 标签，例如 free / paid / test / production。
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// 是否禁用该 Key。
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后使用时间。
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 是否被收藏。
    /// </summary>
    public bool IsFavorite { get; set; }
}
