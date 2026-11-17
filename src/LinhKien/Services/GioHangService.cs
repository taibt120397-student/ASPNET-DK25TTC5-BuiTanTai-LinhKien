using LinhKien.Data;
using LinhKien.Models;
using LinhKien.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Services;

public record KetQuaGio(bool Ok, string ThongBao, int Dem);

public interface IGioHangService
{
    Task<int> DemAsync(string userId);
    Task<KetQuaGio> ThemAsync(string userId, int sanPhamId, int soLuong);
    Task<KetQuaGio> CapNhatAsync(string userId, int chiTietId, int soLuong);
    Task<KetQuaGio> XoaAsync(string userId, int chiTietId);
    Task<GioHangVM> LayAsync(string userId);
}

/// <summary>Giỏ hàng gắn với tài khoản (bảng GioHang, ChiTietGioHang). Tuần 4 sp_DatHang sẽ đọc giỏ này.</summary>
public class GioHangService : IGioHangService
{
    private readonly AppDbContext _db;

    public GioHangService(AppDbContext db) => _db = db;

    public async Task<int> DemAsync(string userId)
    {
        var gioIds = _db.GioHangs.Where(g => g.NguoiDungId == userId).Select(g => g.Id);
        return await _db.ChiTietGioHangs.CountAsync(c => gioIds.Contains(c.GioHangId));
    }

    public async Task<KetQuaGio> ThemAsync(string userId, int sanPhamId, int soLuong)
    {
        soLuong = Math.Clamp(soLuong, 1, 999);

        var sp = await _db.SanPhams.AsNoTracking()
            .Where(s => s.Id == sanPhamId)
            .Select(s => new { s.TenSP, s.SoLuongTon, s.NgungBan })
            .FirstOrDefaultAsync();

        if (sp == null || sp.NgungBan)
            return new KetQuaGio(false, "Sản phẩm không còn được bán.", await DemAsync(userId));
        if (sp.SoLuongTon <= 0)
            return new KetQuaGio(false, $"“{sp.TenSP}” đã hết hàng.", await DemAsync(userId));

        var gio = await LayHoacTaoGioAsync(userId);
        var ct = await _db.ChiTietGioHangs.FirstOrDefaultAsync(c => c.GioHangId == gio.Id && c.SanPhamId == sanPhamId);

        var moi = (ct?.SoLuong ?? 0) + soLuong;
        string tb;
        if (moi > sp.SoLuongTon)
        {
            moi = sp.SoLuongTon;
            tb = $"Chỉ còn {sp.SoLuongTon} sản phẩm, giỏ hàng đã đặt ở số lượng tối đa.";
        }
        else
        {
            tb = $"Đã thêm “{sp.TenSP}” vào giỏ hàng.";
        }

        if (ct == null)
            _db.ChiTietGioHangs.Add(new ChiTietGioHang { GioHangId = gio.Id, SanPhamId = sanPhamId, SoLuong = moi });
        else
            ct.SoLuong = moi;

        gio.NgayCapNhat = DateTime.Now;
        await _db.SaveChangesAsync();
        return new KetQuaGio(true, tb, await DemAsync(userId));
    }

    public async Task<KetQuaGio> CapNhatAsync(string userId, int chiTietId, int soLuong)
    {
        var ct = await TimDongCuaToiAsync(userId, chiTietId);
        if (ct == null) return new KetQuaGio(false, "Không tìm thấy sản phẩm trong giỏ.", await DemAsync(userId));

        if (soLuong <= 0) return await XoaAsync(userId, chiTietId);

        var ton = await _db.SanPhams.Where(s => s.Id == ct.SanPhamId).Select(s => s.SoLuongTon).FirstOrDefaultAsync();
        string tb = "Đã cập nhật số lượng.";
        if (soLuong > ton)
        {
            soLuong = Math.Max(ton, 1);
            tb = $"Chỉ còn {ton} sản phẩm trong kho, đã điều chỉnh số lượng.";
        }
        ct.SoLuong = Math.Min(soLuong, 999);

        await CapNhatNgayGioAsync(ct.GioHangId);
        await _db.SaveChangesAsync();
        return new KetQuaGio(true, tb, await DemAsync(userId));
    }

    public async Task<KetQuaGio> XoaAsync(string userId, int chiTietId)
    {
        var ct = await TimDongCuaToiAsync(userId, chiTietId);
        if (ct == null) return new KetQuaGio(false, "Không tìm thấy sản phẩm trong giỏ.", await DemAsync(userId));

        _db.ChiTietGioHangs.Remove(ct);
        await CapNhatNgayGioAsync(ct.GioHangId);
        await _db.SaveChangesAsync();
        return new KetQuaGio(true, "Đã xóa sản phẩm khỏi giỏ hàng.", await DemAsync(userId));
    }

    public async Task<GioHangVM> LayAsync(string userId)
    {
        var gioId = await _db.GioHangs.AsNoTracking()
            .Where(g => g.NguoiDungId == userId).Select(g => g.Id).FirstOrDefaultAsync();
        if (gioId == 0) return new GioHangVM();

        var dong = await (from c in _db.ChiTietGioHangs.AsNoTracking()
                          join s in _db.SanPhams.AsNoTracking() on c.SanPhamId equals s.Id
                          where c.GioHangId == gioId
                          orderby c.Id
                          select new GioHangDong
                          {
                              ChiTietId = c.Id,
                              SanPhamId = s.Id,
                              MaSP = s.MaSP,
                              Ten = s.TenSP,
                              Slug = s.Slug,
                              HinhAnh = s.HinhAnh,
                              Gia = s.Gia,
                              GiaKhuyenMai = s.GiaKhuyenMai,
                              SoLuong = c.SoLuong,
                              Ton = s.SoLuongTon,
                              NgungBan = s.NgungBan
                          }).ToListAsync();

        return new GioHangVM { Dong = dong };
    }

    // ---------- hỗ trợ ----------

    private async Task<GioHang> LayHoacTaoGioAsync(string userId)
    {
        var gio = await _db.GioHangs.FirstOrDefaultAsync(g => g.NguoiDungId == userId);
        if (gio != null) return gio;

        gio = new GioHang { NguoiDungId = userId, NgayCapNhat = DateTime.Now };
        _db.GioHangs.Add(gio);
        await _db.SaveChangesAsync();
        return gio;
    }

    /// <summary>Chỉ trả về dòng thuộc giỏ của chính người dùng (chống sửa giỏ người khác).</summary>
    private async Task<ChiTietGioHang?> TimDongCuaToiAsync(string userId, int chiTietId)
    {
        var gioIds = _db.GioHangs.Where(g => g.NguoiDungId == userId).Select(g => g.Id);
        return await _db.ChiTietGioHangs.FirstOrDefaultAsync(c => c.Id == chiTietId && gioIds.Contains(c.GioHangId));
    }

    private async Task CapNhatNgayGioAsync(int gioId)
    {
        var gio = await _db.GioHangs.FirstOrDefaultAsync(g => g.Id == gioId);
        if (gio != null) gio.NgayCapNhat = DateTime.Now;
    }
}
