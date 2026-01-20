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
