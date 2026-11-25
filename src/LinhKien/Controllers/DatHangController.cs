using System.Security.Claims;
using System.Text.RegularExpressions;
using LinhKien.Data;
using LinhKien.Helpers;
using LinhKien.Models;
using LinhKien.Services;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinhKien.Controllers;

/// <summary>Trang thanh toán: chọn địa chỉ, phương thức thanh toán, mã giảm giá rồi đặt hàng.</summary>
[Authorize]
[Route("dat-hang")]
public class DatHangController : Controller
{
    private static readonly Regex DienThoaiRegex = new(@"^(0|\+84)\d{9}$");

    private readonly AppDbContext _db;
    private readonly IGioHangService _gio;
    private readonly IDonHangService _don;
    private readonly CuaHangOptions _opt;

    public DatHangController(AppDbContext db, IGioHangService gio, IDonHangService don, IOptions<CuaHangOptions> opt)
    {
        _db = db;
        _gio = gio;
        _don = don;
        _opt = opt.Value;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var gio = await _gio.LayAsync(UserId);
        var chuyen = KiemTraGio(gio);
        if (chuyen != null) return chuyen;

        var diaChis = await LayDiaChisAsync();
        var form = new DatHangFormVM { DiaChiId = diaChis.FirstOrDefault(d => d.MacDinh)?.Id ?? diaChis.FirstOrDefault()?.Id ?? 0 };
        return View(await DungTrangAsync(form, gio, diaChis));
    }

    [HttpPost(""), ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DatHangFormVM form)
    {
        var gio = await _gio.LayAsync(UserId);
        var chuyen = KiemTraGio(gio);
        if (chuyen != null) return chuyen;

        var diaChis = await LayDiaChisAsync();

        // 1. Xác định thông tin nhận hàng
        string ten, dienThoai, diaChi;
        if (form.DiaChiId > 0)
        {
            var dc = diaChis.FirstOrDefault(d => d.Id == form.DiaChiId);   // chỉ lấy trong sổ địa chỉ của chính người dùng
            if (dc == null)
            {
                ModelState.AddModelError(nameof(form.DiaChiId), "Địa chỉ đã chọn không hợp lệ.");
                return View(await DungTrangAsync(form, gio, diaChis));
            }
            (ten, dienThoai, diaChi) = (dc.HoTenNguoiNhan, dc.DienThoai, dc.DiaChi);
        }
        else
        {
            ten = form.HoTenNguoiNhan?.Trim() ?? "";
            dienThoai = form.DienThoai?.Trim() ?? "";
            diaChi = form.DiaChi?.Trim() ?? "";
            if (ten == "") ModelState.AddModelError(nameof(form.HoTenNguoiNhan), "Nhập họ tên người nhận.");
            if (!DienThoaiRegex.IsMatch(dienThoai)) ModelState.AddModelError(nameof(form.DienThoai), "Số điện thoại không hợp lệ (ví dụ 0912345678).");
            if (diaChi.Length < 10) ModelState.AddModelError(nameof(form.DiaChi), "Địa chỉ cần ghi rõ số nhà, đường, phường/xã, quận/huyện, tỉnh/thành.");
        }

        // 2. Phương thức thanh toán
        if (form.PhuongThucThanhToan != PhuongThucThanhToan.COD && form.PhuongThucThanhToan != PhuongThucThanhToan.ChuyenKhoan)
            ModelState.AddModelError(nameof(form.PhuongThucThanhToan), "Chọn phương thức thanh toán.");

        if (!ModelState.IsValid)
            return View(await DungTrangAsync(form, gio, diaChis));

        // 3. Đặt hàng (sp_DatHang: một transaction)
        var kq = await _don.DatHangAsync(UserId, ten, dienThoai, diaChi, form.PhuongThucThanhToan, form.MaGiamGia, form.GhiChu);

        if (kq.Ok)
        {
            if (form.DiaChiId == 0 && form.LuuDiaChi)
                await LuuDiaChiMoiAsync(ten, dienThoai, diaChi);

            TempData["ThongBao"] = $"Đặt hàng thành công. Mã đơn của bạn là {kq.MaDon}.";
            return RedirectToAction("ChiTietDon", "TaiKhoan", new { id = kq.DonHangId });
        }

        switch (kq.MaLoi)
        {
            case 1:
                TempData["Loi"] = "Giỏ hàng đang trống.";
                return RedirectToAction("Index", "GioHang");
            case 2:
                var ds = string.Join("; ", kq.Thieu.Select(t => t.NgungBan
                    ? $"{t.TenSP} (đã ngừng bán)"
                    : $"{t.TenSP} (cần {t.SoLuongYeuCau}, còn {t.SoLuongCon})"));
                TempData["Loi"] = "Không đặt được hàng vì có sản phẩm không đủ số lượng: " + ds + ". Vui lòng điều chỉnh giỏ hàng.";
                return RedirectToAction("Index", "GioHang");
            case 3:
                ModelState.AddModelError(nameof(form.MaGiamGia), kq.ThongBao);
                break;
            default:
                ModelState.AddModelError("", "Không đặt được hàng: " + kq.ThongBao);
                break;
        }
        return View(await DungTrangAsync(form, gio, diaChis));
    }

    /// <summary>Xem trước tiền hàng, giảm giá, phí vận chuyển, tổng khi nhập mã (AJAX).</summary>
    [HttpPost("tinh-tam"), ValidateAntiForgeryToken]
    public async Task<IActionResult> TinhTam(string? maGiamGia)
    {
        var t = await _don.TamTinhAsync(UserId, maGiamGia);
        return Json(new
        {
            maHopLe = t.MaHopLe,
            thongBaoMa = t.ThongBaoMa,
            tienHang = t.TienHang.Vnd(),
            tienGiam = t.TienGiam.Vnd(),
            phi = t.Phi == 0 ? "Miễn phí" : t.Phi.Vnd(),
            tong = t.Tong.Vnd()
        });
    }

    // ---------- hỗ trợ ----------
    private IActionResult? KiemTraGio(GioHangVM gio)
    {
        if (!gio.Dong.Any())
        {
            TempData["Loi"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "GioHang");
        }
        if (gio.CoCanhBao)
        {
            TempData["Loi"] = "Vui lòng điều chỉnh các sản phẩm hết hàng hoặc ngừng bán trong giỏ trước khi đặt hàng.";
            return RedirectToAction("Index", "GioHang");
        }
        return null;
    }

    private async Task<List<DiaChiItemVM>> LayDiaChisAsync() =>
        await _db.DiaChiGiaoHangs.AsNoTracking()
            .Where(d => d.NguoiDungId == UserId)
            .OrderByDescending(d => d.MacDinh).ThenByDescending(d => d.Id)
            .Select(d => new DiaChiItemVM
            {
                Id = d.Id, HoTenNguoiNhan = d.HoTenNguoiNhan, DienThoai = d.DienThoai, DiaChi = d.DiaChi, MacDinh = d.MacDinh
            }).ToListAsync();

    private async Task<DatHangPageVM> DungTrangAsync(DatHangFormVM form, GioHangVM gio, List<DiaChiItemVM> diaChis) => new()
    {
        Form = form,
        Gio = gio,
        DiaChis = diaChis,
        TamTinh = await _don.TamTinhAsync(UserId, form.MaGiamGia),
        PhiCoDinh = _opt.PhiVanChuyen,
        NguongMienPhi = _opt.NguongMienPhi,
        NganHang = _opt.NganHang,
        SoTaiKhoan = _opt.SoTaiKhoan,
        ChuTaiKhoan = _opt.ChuTaiKhoan
    };

    private async Task LuuDiaChiMoiAsync(string ten, string dienThoai, string diaChi)
    {
        var daCo = await _db.DiaChiGiaoHangs.AnyAsync(d => d.NguoiDungId == UserId);
        _db.DiaChiGiaoHangs.Add(new DiaChiGiaoHang
        {
            NguoiDungId = UserId,
            HoTenNguoiNhan = ten,
            DienThoai = dienThoai,
            DiaChi = diaChi,
            MacDinh = !daCo          // địa chỉ đầu tiên tự là mặc định
        });
        await _db.SaveChangesAsync();
    }
}
