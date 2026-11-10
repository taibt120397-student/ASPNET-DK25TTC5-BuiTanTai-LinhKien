using System.Globalization;

namespace LinhKien.Helpers;

public static class MoneyExtensions
{
    private static readonly CultureInfo Vn = new("vi-VN");

    public static string Vnd(this decimal v) => v.ToString("N0", Vn) + " ₫";
    public static string Vnd(this decimal? v) => v.HasValue ? v.Value.Vnd() : "";
}
