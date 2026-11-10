using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LinhKien.Helpers;

public static class SlugHelper
{
    /// <summary>"Cảm biến nhiệt độ - độ ẩm" -> "cam-bien-nhiet-do-do-am"</summary>
    public static string ToSlug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var s = text.Trim().ToLowerInvariant().Replace('đ', 'd');
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        s = sb.ToString().Normalize(NormalizationForm.FormC);
        s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
        s = Regex.Replace(s, @"[\s-]+", "-").Trim('-');
        return s;
    }
}
