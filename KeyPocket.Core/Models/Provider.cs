using System;
using System.Collections.Generic;

namespace KeyPocket.Core.Models;

/// <summary>
/// 模型服务商。
/// </summary>
public class Provider
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    /// <summary>
    /// 服务商类型 (e.g. "OpenAI Compatible", "Anthropic").
    /// </summary>
    public string Type { get; set; } = "OpenAI Compatible";

    /// <summary>
    /// API 基础地址。
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// 该服务商关联的 API Keys。
    /// </summary>
    public List<ApiKey> ApiKeys { get; set; } = new();

    /// <summary>
    /// 该服务商支持的模型。
    /// </summary>
    public List<ModelInfo> Models { get; set; } = new();

    /// <summary>
    /// 收藏的模型 ID 列表。
    /// </summary>
    public HashSet<string> FavoriteModelIds { get; set; } = new();

    /// <summary>
    /// 收藏的 API Key ID 列表。
    /// </summary>
    public HashSet<Guid> FavoriteApiKeyIds { get; set; } = new();

    /// <summary>
    /// 自定义图标的相对路径 (e.g. "Icons/my-icon.png")。
    /// </summary>
    public string? IconPath { get; set; }
}
