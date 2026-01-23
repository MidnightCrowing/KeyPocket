using System;

namespace KeyPocket.Core.Models;

/// <summary>
/// AI 模型信息。
/// </summary>
public class ModelInfo
{
    public string Id { get; set; } = string.Empty;

    public Guid ProviderId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 每百万输入 Token 的价格。
    /// </summary>
    public decimal? InputPricePerMTokens { get; set; }

    /// <summary>
    /// 每百万输出 Token 的价格。
    /// </summary>
    public decimal? OutputPricePerMTokens { get; set; }

    /// <summary>
    /// 保存时使用的货币（例如 "USD" 或 "CNY"）。新增字段，便于在不同显示货币间进行转换。
    /// 如果为 null 或空，默认视为 "USD"。
    /// </summary>
    public string? PriceCurrency { get; set; }

    /// <summary>
    /// 模型是否已弃用。
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// 模型是否为聊天模型。
    /// </summary>
    public bool IsChatModel { get; set; }

    /// <summary>
    /// 模型是否为嵌入模型。
    /// </summary>
    public bool IsEmbeddingModel { get; set; }

    /// <summary>
    /// 模型是否被收藏。
    /// </summary>
    public bool IsFavorite { get; set; }
}
