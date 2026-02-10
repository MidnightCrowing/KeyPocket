namespace KeyPocket.Core.Models;

/// <summary>
///     AI 模型信息。
/// </summary>
public class ModelInfo
{
    public string Id { get; set; } = string.Empty;

    public Guid ProviderId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     每百万输入 Token 的价格。
    /// </summary>
    public decimal? InputPricePerMTokens { get; set; }

    /// <summary>
    ///     每百万输出 Token 的价格。
    /// </summary>
    public decimal? OutputPricePerMTokens { get; set; }

    /// <summary>
    ///     模型标签集合。
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();
}

/// <summary>
///     模型标签常量定义。
/// </summary>
public static class ModelTags
{
    /// <summary>文本/聊天能力</summary>
    public const string Text = "Text";

    /// <summary>文件处理能力</summary>
    public const string File = "File";

    /// <summary>图像处理能力</summary>
    public const string Image = "Image";

    /// <summary>音频处理能力</summary>
    public const string Audio = "Audio";

    /// <summary>视频处理能力</summary>
    public const string Video = "Video";

    /// <summary>嵌入模型</summary>
    public const string Embeddings = "Embeddings";

    /// <summary>收藏</summary>
    public const string Favorite = "Favorite";

    /// <summary>已弃用</summary>
    public const string Deprecated = "Deprecated";
}