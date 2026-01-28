using System;

namespace KeyPocket.UI.Models;

public enum SearchResultType
{
    Provider,
    Model,
    SystemFile
}

public enum IconType
{
    Glyph, // 使用 FontIcon
    ImagePath // 使用 ImageIcon
}

public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SearchResultType Type { get; set; }
    public object? Data { get; set; }
    public string Icon { get; set; } = string.Empty;
    public IconType IconKind { get; set; } = IconType.Glyph;
    public Uri? IconPath { get; set; } // 使用 Uri 类型
    public bool IsSystemFile => Type == SearchResultType.SystemFile;
    public bool IsGlyphIcon => IconKind == IconType.Glyph;
    public bool IsImageIcon => IconKind == IconType.ImagePath;
}