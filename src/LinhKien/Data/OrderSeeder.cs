using LinhKien.Models;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Data;

/// <summary>Mã giảm giá mẫu để thử chức năng. Chỉ chạy khi bảng MaGiamGia còn trống.</summary>
public static class OrderSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.MaGiamGias.AnyAsync()) return;

        var homNay = DateTime.Today;
        var hetNam = homNay.AddYears(1).AddDays(1).AddSeconds(-1);

        db.MaGiamGias.AddRange(
            new MaGiamGia { Ma = "GIAM10", LaPhanTram = true, GiaTriGiam = 10, DonToiThieu = 200000, NgayBatDau = homNay, NgayKetThuc = hetNam, SoLanToiDa = 100, KichHoat = true },
            new MaGiamGia { Ma = "GIAM50K", LaPhanTram = false, GiaTriGiam = 50000, DonToiThieu = 300000, NgayBatDau = homNay, NgayKetThuc = hetNam, SoLanToiDa = 50, KichHoat = true },
            // Hai mã dưới đây dùng để thử các tình huống lỗi: mã hết hạn và mã bị khóa
            new MaGiamGia { Ma = "HETHAN", LaPhanTram = true, GiaTriGiam = 20, DonToiThieu = 0, NgayBatDau = homNay.AddMonths(-2), NgayKetThuc = homNay.AddDays(-1), SoLanToiDa = 10, KichHoat = true },
            new MaGiamGia { Ma = "DAKHOA", LaPhanTram = false, GiaTriGiam = 100000, DonToiThieu = 0, NgayBatDau = homNay, NgayKetThuc = hetNam, SoLanToiDa = 10, KichHoat = false });

        await db.SaveChangesAsync();
    }
}
