using System.Security.Claims;
using LinhKien.Data;
using LinhKien.Helpers;
using LinhKien.Models;
using LinhKien.Services;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Controllers;

/// <summary>Khu vực tài khoản của khách hàng: hồ sơ, sổ địa chỉ, theo dõi và hủy đơn.</summary>
[Authorize]
[Route("tai-khoan")]
public class TaiKhoanController : Controller
{
    private const int PageSize = 10;

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IDonHangService _don;

    public TaiKhoanController(AppDbContext db, UserManager<ApplicationUser> users, IDonHangService don)
    {
        _db = db;
        _users = users;
        _don = don;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ================= HỒ SƠ =================
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var u = await _users.GetUserAsync(User);
        if (u == null) return Challenge();
        ViewData["Active"] = "hoso";
        return View(new HoSoVM { HoTen = u.HoTen, PhoneNumber = u.PhoneNumber, Email = u.Email });
    }

    [HttpPost(""), ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HoSoVM vm)
    {
        var u = await _users.GetUserAsync(User);
        if (u == null) return Challenge();
        ViewData["Active"] = "hoso";
        vm.Email = u.Email;
        if (!ModelState.IsValid) return View(vm);

        u.HoTen = vm.HoTen?.Trim();
        u.PhoneNumber = string.IsNullOrWhiteSpace(vm.PhoneNumber) ? null : vm.PhoneNumber.Trim();
        var kq = await _users.UpdateAsync(u);
        if (!kq.Succeeded)
        {
            foreach (var e in kq.Errors) ModelState.AddModelError("", e.Description);
            return View(vm);
        }
        TempData["ThongBao"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Index));
    }

    // ================= ĐƠN HÀNG CỦA TÔI =================
    [HttpGet("don-hang")]
    public async Task<IActionResult> DonHang(string? trangThai, int trang = 1)
    {
        var q = _db.DonHangs.AsNoTracking().Where(d => d.NguoiDungId == UserId);
        if (!string.IsNullOrEmpty(trangThai)) q = q.Where(d => d.TrangThai == trangThai);

        var tong = await q.CountAsync();
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tong / (double)PageSize));
        trang = Math.Clamp(trang, 1, tongTrang);

        var rows = await q.OrderByDescending(d => d.NgayDat).ThenByDescending(d => d.Id)
            .Skip((trang - 1) * PageSize).Take(PageSize)
            .Select(d => new DonHangRowVM
            {
                Id = d.Id, MaDon = d.MaDon, NgayDat = d.NgayDat, TrangThai = d.TrangThai,
                PhuongThucThanhToan = d.PhuongThucThanhToan, TrangThaiThanhToan = d.TrangThaiThanhToan,
                TenNguoiNhan = d.TenNguoiNhan, DienThoai = d.DienThoai, TongTien = d.TongTien,
                SoMon = d.ChiTiets.Sum(c => c.SoLuong)
            }).ToListAsync();

        ViewData["Active"] = "don";
        return View(new DonHangListVM { Rows = rows, TrangThai = trangThai, Trang = trang, TongTrang = tongTrang, TongSo = tong });
    }

    [HttpGet("don-hang/{id:int}")]
    public async Task<IActionResult> ChiTietDon(int id)
    {
        var vm = await _db.DonHangs.AsNoTracking()
            .Where(d => d.Id == id && d.NguoiDungId == UserId)      // chỉ xem được đơn của chính mình
            .Select(d => new DonHangChiTietVM
            {
                Id = d.Id, MaDon = d.MaDon, NgayDat = d.NgayDat, TrangThai = d.TrangThai,
                PhuongThucThanhToan = d.PhuongThucThanhToan, TrangThaiThanhToan = d.TrangThaiThanhToan,
                TenNguoiNhan = d.TenNguoiNhan, DienThoai = d.DienThoai, DiaChiGiao = d.DiaChiGiao, GhiChu = d.GhiChu,
                PhiVanChuyen = d.PhiVanChuyen, TienGiam = d.TienGiam, TongTien = d.TongTien,
                MaGiamGia = d.MaGiamGia != null ? d.MaGiamGia.Ma : null
            }).FirstOrDefaultAsync();
        if (vm == null) return NotFound();

        vm.Dong = await _db.ChiTietDonHangs.AsNoTracking().Where(c => c.DonHangId == id).OrderBy(c => c.Id)
            .Select(c => new DonHangDongVM
            {
                SanPhamId = c.SanPhamId, MaSP = c.SanPham!.MaSP, TenSP = c.SanPham.TenSP, Slug = c.SanPham.Slug,
                HinhAnh = c.SanPham.HinhAnh, SoLuong = c.SoLuong, DonGia = c.DonGia
            }).ToListAsync();

        ViewData["Active"] = "don";
        return View(vm);
    }

    [HttpPost("don-hang/{id:int}/huy"), ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyDon(int id)
    {
        var kq = await _don.HuyAsync(id, UserId, laNhanVien: false);
        TempData[kq.Ok ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTietDon), new { id });
    }

    // ================= SỔ ĐỊA CHỈ =================
    [HttpGet("dia-chi")]
    public async Task<IActionResult> DiaChi()
    {
        var list = await _db.DiaChiGiaoHangs.AsNoTracking().Where(d => d.NguoiDungId == UserId)
            .OrderByDescending(d => d.MacDinh).ThenByDescending(d => d.Id)
            .Select(d => new DiaChiItemVM
            {
                Id = d.Id, HoTenNguoiNhan = d.HoTenNguoiNhan, DienThoai = d.DienThoai, DiaChi = d.DiaChi, MacDinh = d.MacDinh
            }).ToListAsync();
        ViewData["Active"] = "diachi";
        return View(list);
    }

    [HttpGet("dia-chi/tao")]
    public IActionResult TaoDiaChi()
    {
        ViewData["Active"] = "diachi";
        return View("FormDiaChi", new DiaChiFormVM());
    }

    [HttpPost("dia-chi/tao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoDiaChi(DiaChiFormVM vm)
    {
        ViewData["Active"] = "diachi";
        if (!ModelState.IsValid) return View("FormDiaChi", vm);

        var daCo = await _db.DiaChiGiaoHangs.AnyAsync(d => d.NguoiDungId == UserId);
        var moi = new DiaChiGiaoHang
        {
            NguoiDungId = UserId,
            HoTenNguoiNhan = vm.HoTenNguoiNhan.Trim(),
            DienThoai = vm.DienThoai.Trim(),
            DiaChi = vm.DiaChi.Trim(),
            MacDinh = vm.MacDinh || !daCo
        };
        if (moi.MacDinh) await BoMacDinhKhacAsync(0);
        _db.DiaChiGiaoHangs.Add(moi);
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = "Đã thêm địa chỉ giao hàng.";
        return RedirectToAction(nameof(DiaChi));
    }

    [HttpGet("dia-chi/{id:int}/sua")]
    public async Task<IActionResult> SuaDiaChi(int id)
    {
        var d = await _db.DiaChiGiaoHangs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == UserId);
        if (d == null) return NotFound();
        ViewData["Active"] = "diachi";
        return View("FormDiaChi", new DiaChiFormVM
        {
            Id = d.Id, HoTenNguoiNhan = d.HoTenNguoiNhan, DienThoai = d.DienThoai, DiaChi = d.DiaChi, MacDinh = d.MacDinh
        });
    }

    [HttpPost("dia-chi/{id:int}/sua"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaDiaChi(int id, DiaChiFormVM vm)
    {
        vm.Id = id;
        var d = await _db.DiaChiGiaoHangs.FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == UserId);
        if (d == null) return NotFound();
        ViewData["Active"] = "diachi";
        if (!ModelState.IsValid) return View("FormDiaChi", vm);

        d.HoTenNguoiNhan = vm.HoTenNguoiNhan.Trim();
        d.DienThoai = vm.DienThoai.Trim();
        d.DiaChi = vm.DiaChi.Trim();
        if (vm.MacDinh && !d.MacDinh)
        {
            await BoMacDinhKhacAsync(id);
            d.MacDinh = true;
        }
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = "Đã cập nhật địa chỉ giao hàng.";
        return RedirectToAction(nameof(DiaChi));
    }

    [HttpPost("dia-chi/{id:int}/mac-dinh"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DatMacDinh(int id)
    {
        var d = await _db.DiaChiGiaoHangs.FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == UserId);
        if (d == null) return NotFound();
        await BoMacDinhKhacAsync(id);
        d.MacDinh = true;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = "Đã đặt làm địa chỉ mặc định.";
        return RedirectToAction(nameof(DiaChi));
    }

    [HttpPost("dia-chi/{id:int}/xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaDiaChi(int id)
    {
        var d = await _db.DiaChiGiaoHangs.FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == UserId);
        if (d == null) return NotFound();

        var laMacDinh = d.MacDinh;
        _db.DiaChiGiaoHangs.Remove(d);
        await _db.SaveChangesAsync();

        if (laMacDinh)   // xóa địa chỉ mặc định thì chuyển mặc định sang địa chỉ mới nhất còn lại
        {
            var khac = await _db.DiaChiGiaoHangs.Where(x => x.NguoiDungId == UserId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            if (khac != null) { khac.MacDinh = true; await _db.SaveChangesAsync(); }
        }
        TempData["ThongBao"] = "Đã xóa địa chỉ.";
        return RedirectToAction(nameof(DiaChi));
    }

    private async Task BoMacDinhKhacAsync(int giuLaiId)
    {
        var cacDiaChi = await _db.DiaChiGiaoHangs.Where(x => x.NguoiDungId == UserId && x.MacDinh && x.Id != giuLaiId).ToListAsync();
        foreach (var x in cacDiaChi) x.MacDinh = false;
    }
}
