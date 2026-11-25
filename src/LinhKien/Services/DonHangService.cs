using System.Data;
using LinhKien.Data;
using LinhKien.Helpers;
using LinhKien.Models;
using LinhKien.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinhKien.Services;

/// <summary>Cấu hình cửa hàng, đọc từ mục "CuaHang" trong appsettings.json (không có thì dùng giá trị mặc định).</summary>
public class CuaHangOptions
{
    public decimal PhiVanChuyen { get; set; } = 30000;       // phí cố định cho mỗi đơn
    public decimal NguongMienPhi { get; set; } = 500000;     // tiền hàng đạt mức này thì miễn phí vận chuyển
    public string NganHang { get; set; } = "Vietcombank";
    public string SoTaiKhoan { get; set; } = "0123456789";
    public string ChuTaiKhoan { get; set; } = "CONG TY LINHKIEN";
}

public class SanPhamThieu
{
    public int SanPhamId { get; set; }
    public string TenSP { get; set; } = "";
    public int SoLuongYeuCau { get; set; }
    public int SoLuongCon { get; set; }
    public bool NgungBan { get; set; }
}

public class KetQuaDatHang
{
    public bool Ok => MaLoi == 0;
    public int MaLoi { get; set; }
    public string ThongBao { get; set; } = "";
    public int? DonHangId { get; set; }
    public string? MaDon { get; set; }
    public List<SanPhamThieu> Thieu { get; set; } = new();
}

public record KetQuaThaoTac(bool Ok, string ThongBao);

public interface IDonHangService
{
    Task<TamTinhVM> TamTinhAsync(string userId, string? maGiamGia);
    Task<KetQuaDatHang> DatHangAsync(string userId, string tenNguoiNhan, string dienThoai, string diaChi,
                                     string phuongThuc, string? maGiamGia, string? ghiChu);
    Task<KetQuaThaoTac> HuyAsync(int donHangId, string nguoiThucHienId, bool laNhanVien);
    Task<KetQuaThaoTac> DoiTrangThaiAsync(int donHangId, string trangThaiMoi, string nguoiThucHienId);
    Task<KetQuaThaoTac> XacNhanThanhToanAsync(int donHangId, string nguoiThucHienId);
}

public class DonHangService : IDonHangService
{
    private readonly AppDbContext _db;
    private readonly CuaHangOptions _opt;

    public DonHangService(AppDbContext db, IOptions<CuaHangOptions> opt)
    {
        _db = db;
        _opt = opt.Value;
    }

    // ================= TẠM TÍNH (xem trước, chưa tạo đơn) =================
    public async Task<TamTinhVM> TamTinhAsync(string userId, string? maGiamGia)
    {
        var gioIds = _db.GioHangs.Where(g => g.NguoiDungId == userId).Select(g => g.Id);
        var tienHang = await (from c in _db.ChiTietGioHangs
                              join s in _db.SanPhams on c.SanPhamId equals s.Id
                              where gioIds.Contains(c.GioHangId)
                              select (decimal?)(c.SoLuong * (s.GiaKhuyenMai ?? s.Gia))).SumAsync() ?? 0m;

        var kq = new TamTinhVM { TienHang = tienHang };

        var ma = maGiamGia?.Trim();
        if (!string.IsNullOrEmpty(ma))
        {
            var mg = await _db.MaGiamGias.AsNoTracking().FirstOrDefaultAsync(m => m.Ma == ma);
            var loi = KiemTraMa(mg, tienHang);
            if (loi != null)
            {
                kq.ThongBaoMa = loi;
            }
            else
            {
                kq.MaHopLe = true;
                kq.TienGiam = TinhGiam(mg!, tienHang);
                kq.ThongBaoMa = $"Đã áp dụng mã {mg!.Ma}.";
            }
        }

        kq.Phi = tienHang <= 0 || tienHang >= _opt.NguongMienPhi ? 0 : _opt.PhiVanChuyen;
        kq.Tong = tienHang - kq.TienGiam + kq.Phi;
        return kq;
    }

    /// <summary>Quy tắc kiểm tra mã giống sp_DatHang (sp_DatHang vẫn kiểm tra lại trong transaction).</summary>
    private static string? KiemTraMa(MaGiamGia? m, decimal tienHang)
    {
        if (m == null || !m.KichHoat) return "Mã giảm giá không tồn tại hoặc đã bị khóa.";
        var now = DateTime.Now;
        if (now < m.NgayBatDau) return "Mã giảm giá chưa đến thời gian áp dụng.";
        if (now > m.NgayKetThuc) return "Mã giảm giá đã hết hạn.";
        if (m.SoLanDaDung >= m.SoLanToiDa) return "Mã giảm giá đã hết lượt sử dụng.";
        if (tienHang < m.DonToiThieu) return $"Đơn hàng cần đạt tối thiểu {m.DonToiThieu.Vnd()} để dùng mã này.";
        return null;
    }

    private static decimal TinhGiam(MaGiamGia m, decimal tienHang)
    {
        var giam = m.LaPhanTram
            ? Math.Round(tienHang * m.GiaTriGiam / 100m, 0, MidpointRounding.AwayFromZero)
            : m.GiaTriGiam;
        return Math.Min(giam, tienHang);
    }

    // ================= ĐẶT HÀNG: gọi sp_DatHang =================
    public async Task<KetQuaDatHang> DatHangAsync(string userId, string tenNguoiNhan, string dienThoai, string diaChi,
                                                  string phuongThuc, string? maGiamGia, string? ghiChu)
    {
        var pId = new SqlParameter("@DonHangId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var pMaDon = new SqlParameter("@MaDon", SqlDbType.NVarChar, 30) { Direction = ParameterDirection.Output };
        var pMaLoi = new SqlParameter("@MaLoi", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var pThongBao = new SqlParameter("@ThongBao", SqlDbType.NVarChar, 300) { Direction = ParameterDirection.Output };

        var kq = new KetQuaDatHang();

        await _db.Database.OpenConnectionAsync();
        try
        {
            await using var cmd = _db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.sp_DatHang";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(Nvarchar("@NguoiDungId", userId, 450));
            cmd.Parameters.Add(Nvarchar("@TenNguoiNhan", tenNguoiNhan, 100));
            cmd.Parameters.Add(Nvarchar("@DienThoai", dienThoai, 20));
            cmd.Parameters.Add(Nvarchar("@DiaChiGiao", diaChi, 300));
            cmd.Parameters.Add(Nvarchar("@PhuongThucThanhToan", phuongThuc, 20));
            cmd.Parameters.Add(Nvarchar("@MaGiamGia", string.IsNullOrWhiteSpace(maGiamGia) ? null : maGiamGia.Trim(), 30));
            cmd.Parameters.Add(Nvarchar("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim(), 500));
            cmd.Parameters.Add(Tien("@PhiCoDinh", _opt.PhiVanChuyen));
            cmd.Parameters.Add(Tien("@NguongMienPhi", _opt.NguongMienPhi));
            cmd.Parameters.Add(pId);
            cmd.Parameters.Add(pMaDon);
            cmd.Parameters.Add(pMaLoi);
            cmd.Parameters.Add(pThongBao);

            // Result set đầu tiên (nếu có) là danh sách sản phẩm thiếu hàng
            await using (var rd = await cmd.ExecuteReaderAsync())
            {
                while (await rd.ReadAsync())
                {
                    kq.Thieu.Add(new SanPhamThieu
                    {
                        SanPhamId = rd.GetInt32(0),
                        TenSP = rd.GetString(1),
                        SoLuongYeuCau = rd.GetInt32(2),
                        SoLuongCon = rd.GetInt32(3),
                        NgungBan = rd.GetBoolean(4)
                    });
                }
            }   // phải đóng reader rồi mới đọc được tham số OUTPUT

            kq.MaLoi = pMaLoi.Value is int loi ? loi : 5;
            kq.ThongBao = pThongBao.Value as string ?? "";
            kq.DonHangId = pId.Value is int id ? id : null;
            kq.MaDon = pMaDon.Value as string;
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
        return kq;
    }

    // ================= HỦY ĐƠN: gọi sp_HuyDonHang =================
    public async Task<KetQuaThaoTac> HuyAsync(int donHangId, string nguoiThucHienId, bool laNhanVien)
    {
        var pMaLoi = new SqlParameter("@MaLoi", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var pThongBao = new SqlParameter("@ThongBao", SqlDbType.NVarChar, 300) { Direction = ParameterDirection.Output };

        await _db.Database.OpenConnectionAsync();
        try
        {
            await using var cmd = _db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "dbo.sp_HuyDonHang";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@DonHangId", SqlDbType.Int) { Value = donHangId });
            // Khách: truyền id để thủ tục kiểm tra chủ đơn. Nhân viên: không cần.
            cmd.Parameters.Add(Nvarchar("@NguoiDungId", laNhanVien ? null : nguoiThucHienId, 450));
            cmd.Parameters.Add(new SqlParameter("@LaNhanVien", SqlDbType.Bit) { Value = laNhanVien });
            cmd.Parameters.Add(pMaLoi);
            cmd.Parameters.Add(pThongBao);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }

        var ok = pMaLoi.Value is int m && m == 0;
        var tb = pThongBao.Value as string ?? "Không hủy được đơn hàng.";
        if (ok)
        {
            var maDon = await _db.DonHangs.AsNoTracking().Where(d => d.Id == donHangId).Select(d => d.MaDon).FirstOrDefaultAsync();
            await GhiNhatKyAsync(nguoiThucHienId, "HuyDon", maDon, null, TrangThaiDon.DaHuy);
        }
        return new KetQuaThaoTac(ok, tb);
    }

    // ================= ĐỔI TRẠNG THÁI (nhân viên) =================
    public async Task<KetQuaThaoTac> DoiTrangThaiAsync(int donHangId, string trangThaiMoi, string nguoiThucHienId)
    {
        var don = await _db.DonHangs.AsNoTracking().Where(d => d.Id == donHangId)
            .Select(d => new { d.MaDon, d.TrangThai, d.PhuongThucThanhToan, d.TrangThaiThanhToan })
            .FirstOrDefaultAsync();
        if (don == null) return new KetQuaThaoTac(false, "Không tìm thấy đơn hàng.");

        // Hủy phải đi qua sp_HuyDonHang để hoàn tồn
        if (trangThaiMoi == TrangThaiDon.DaHuy)
            return await HuyAsync(donHangId, nguoiThucHienId, true);

        var tiepTheo = DonHangHelper.TiepTheo(don.TrangThai);
        if (don.TrangThai == TrangThaiDon.HoanThanh)
            return new KetQuaThaoTac(false, "Đơn hàng đã hoàn thành, không được sửa.");
        if (don.TrangThai == TrangThaiDon.DaHuy)
            return new KetQuaThaoTac(false, "Đơn hàng đã hủy, không được sửa.");
        if (tiepTheo == null || tiepTheo != trangThaiMoi)
            return new KetQuaThaoTac(false,
                $"Không thể chuyển từ \"{DonHangHelper.TenTrangThai(don.TrangThai)}\" sang \"{DonHangHelper.TenTrangThai(trangThaiMoi)}\".");

        // Chuyển khoản: phải xác nhận đã nhận tiền trước khi giao
        if (trangThaiMoi == TrangThaiDon.DangGiao
            && don.PhuongThucThanhToan == PhuongThucThanhToan.ChuyenKhoan
            && don.TrangThaiThanhToan != TrangThaiThanhToan.DaThanhToan)
            return new KetQuaThaoTac(false, "Đơn chuyển khoản chưa được xác nhận thanh toán. Hãy xác nhận đã nhận tiền trước khi giao.");

        // Cập nhật có điều kiện: chỉ thành công nếu trạng thái chưa bị người khác đổi
        var cu = don.TrangThai;
        int n;
        if (trangThaiMoi == TrangThaiDon.HoanThanh && don.PhuongThucThanhToan == PhuongThucThanhToan.COD)
        {
            // COD: thu tiền khi giao xong
            n = await _db.DonHangs.Where(d => d.Id == donHangId && d.TrangThai == cu)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.TrangThai, trangThaiMoi)
                    .SetProperty(d => d.TrangThaiThanhToan, TrangThaiThanhToan.DaThanhToan));
        }
        else
        {
            n = await _db.DonHangs.Where(d => d.Id == donHangId && d.TrangThai == cu)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.TrangThai, trangThaiMoi));
        }

        if (n == 0) return new KetQuaThaoTac(false, "Đơn hàng vừa được người khác cập nhật. Vui lòng tải lại trang.");

        await GhiNhatKyAsync(nguoiThucHienId, "DoiTrangThaiDon", don.MaDon, cu, trangThaiMoi);
        return new KetQuaThaoTac(true, $"Đơn {don.MaDon} chuyển sang \"{DonHangHelper.TenTrangThai(trangThaiMoi)}\".");
    }

    public async Task<KetQuaThaoTac> XacNhanThanhToanAsync(int donHangId, string nguoiThucHienId)
    {
        var don = await _db.DonHangs.AsNoTracking().Where(d => d.Id == donHangId)
            .Select(d => new { d.MaDon, d.TrangThai, d.PhuongThucThanhToan, d.TrangThaiThanhToan })
            .FirstOrDefaultAsync();
        if (don == null) return new KetQuaThaoTac(false, "Không tìm thấy đơn hàng.");
        if (don.TrangThai == TrangThaiDon.DaHuy) return new KetQuaThaoTac(false, "Đơn hàng đã hủy.");
        if (don.PhuongThucThanhToan != PhuongThucThanhToan.ChuyenKhoan)
            return new KetQuaThaoTac(false, "Chỉ đơn chuyển khoản mới cần xác nhận thanh toán thủ công.");
        if (don.TrangThaiThanhToan == TrangThaiThanhToan.DaThanhToan)
            return new KetQuaThaoTac(false, "Đơn hàng đã được xác nhận thanh toán.");

        var n = await _db.DonHangs.Where(d => d.Id == donHangId && d.TrangThaiThanhToan == TrangThaiThanhToan.ChuaThanhToan)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.TrangThaiThanhToan, TrangThaiThanhToan.DaThanhToan));
        if (n == 0) return new KetQuaThaoTac(false, "Đơn hàng vừa được người khác cập nhật. Vui lòng tải lại trang.");

        await GhiNhatKyAsync(nguoiThucHienId, "XacNhanThanhToan", don.MaDon, TrangThaiThanhToan.ChuaThanhToan, TrangThaiThanhToan.DaThanhToan);
        return new KetQuaThaoTac(true, $"Đã xác nhận thanh toán cho đơn {don.MaDon}.");
    }

    // ================= HỖ TRỢ =================
    private async Task GhiNhatKyAsync(string nguoiDungId, string hanhDong, string? khoa, string? cu, string? moi)
    {
        _db.NhatKys.Add(new NhatKyThaoTac
        {
            ThoiGian = DateTime.Now,
            NguoiDungId = nguoiDungId,
            HanhDong = hanhDong,
            BangTacDong = "DonHang",
            KhoaBanGhi = khoa,
            GiaTriCu = cu,
            GiaTriMoi = moi
        });
        await _db.SaveChangesAsync();
    }

    private static SqlParameter Nvarchar(string ten, string? giaTri, int size) =>
        new(ten, SqlDbType.NVarChar, size) { Value = (object?)giaTri ?? DBNull.Value };

    private static SqlParameter Tien(string ten, decimal giaTri) =>
        new(ten, SqlDbType.Decimal) { Precision = 18, Scale = 0, Value = giaTri };
}
