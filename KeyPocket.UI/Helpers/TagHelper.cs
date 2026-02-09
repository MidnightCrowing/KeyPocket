using System;
using Windows.UI;
using Microsoft.UI;

namespace KeyPocket.UI.Helpers;

public static class TagHelper
{
    public static string GetTagColor(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return ThemeHelper.IsDarkTheme() ? "#9CA3AF" : "#6B7280"; // 默认灰色

        var tagLower = tag.ToLower();
        var isDark = ThemeHelper.IsDarkTheme();

        // 开发/测试相关
        if (tagLower.Contains("dev") || tagLower.Contains("开发") || tagLower.Contains("test") ||
            tagLower.Contains("测试"))
            return isDark ? "#60A5FA" : "#3B82F6"; // 蓝色

        // 生产/正式相关
        if (tagLower.Contains("prod") || tagLower.Contains("生产") || tagLower.Contains("正式") ||
            tagLower.Contains("production"))
            return isDark ? "#34D399" : "#10B981"; // 绿色

        // 免费相关
        if (tagLower.Contains("free") || tagLower.Contains("免费") || tagLower.Contains("trial") ||
            tagLower.Contains("试用"))
            return isDark ? "#A78BFA" : "#8B5CF6"; // 紫色

        // 收费/付费相关
        if (tagLower.Contains("paid") || tagLower.Contains("收费") || tagLower.Contains("付费") ||
            tagLower.Contains("premium"))
            return isDark ? "#FBBF24" : "#F59E0B"; // 黄色

        // 临时/暂存相关
        if (tagLower.Contains("temp") || tagLower.Contains("临时") || tagLower.Contains("暂存") ||
            tagLower.Contains("staging"))
            return isDark ? "#FB923C" : "#F97316"; // 橙色

        // 备份相关
        if (tagLower.Contains("backup") || tagLower.Contains("备份") || tagLower.Contains("bak"))
            return isDark ? "#94A3B8" : "#64748B"; // 石板灰

        // 默认颜色
        return isDark ? "#9CA3AF" : "#6B7280"; // 灰色
    }

    public static Color ParseHexColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Colors.Gray;
        var value = hex.Trim().TrimStart('#');
        if (value.Length != 6) return Colors.Gray;

        byte r = Convert.ToByte(value.Substring(0, 2), 16);
        byte g = Convert.ToByte(value.Substring(2, 2), 16);
        byte b = Convert.ToByte(value.Substring(4, 2), 16);
        return Color.FromArgb(255, r, g, b);
    }
}
