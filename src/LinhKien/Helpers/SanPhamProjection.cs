using System.Linq.Expressions;
using LinhKien.Data;
using LinhKien.Models;
using LinhKien.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Helpers;

public static class SanPhamProjection
{
    /// <summary>Chuyển SanPham thành thẻ hiển thị ngay trong câu SQL (chỉ lấy cột cần dùng).</summary>
    public static readonly Expression<Func<SanPham, SanPhamCardVM>> Card = s => new SanPhamCardVM
    {
        Id = s.Id,
        MaSP = s.MaSP,
        Ten = s.TenSP,
        Slug = s.Slug,
        HinhAnh = s.HinhAnh,
        ThuongHieuId = s.ThuongHieuId,
        Gia = s.Gia,
        GiaKhuyenMai = s.GiaKhuyenMai,
        SoLuongTon = s.SoLuongTon,
        MucTonToiThieu = s.MucTonToiThieu
    };

    /// <summary>Điền tên thương hiệu cho các thẻ (bảng thương hiệu nhỏ nên đọc một lần).</summary>
    public static async Task GanThuongHieuAsync(AppDbContext db, params IEnumerable<SanPhamCardVM>[] nhom)
    {
        var ten = await db.ThuongHieus.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.TenThuongHieu);
        foreach (var n in nhom)
            foreach (var c in n)
                c.ThuongHieuTen = ten.GetValueOrDefault(c.ThuongHieuId);
    }
}
