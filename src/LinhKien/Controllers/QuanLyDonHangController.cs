using System.Security.Claims;
using LinhKien.Data;
using LinhKien.Models;
using LinhKien.Services;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Controllers;

[Authorize(Roles = "Admin,NhanVien")]
[Route("quan-tri/don-hang")]
public class QuanLyDonHangController : Controller
{
    private const int PageSize = 15;

    private readonly AppDbContext _db;
    private readonly IDonHangService _don;

    public QuanLyDonHangController(AppDbContext db, IDonHangService don)
    {
        _db = db;
        _don = don;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ================= DANH SÁCH =================
    [HttpGet("")]
    public async Task<IActionResult> Index(string? trangThai, string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, int trang = 1)
    {
        var q = _db.DonHangs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(trangThai)) q = q.Where(d => d.TrangThai == trangThai);
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var k = tuKhoa.Trim();
            q = q.Where(d => d.MaDon.Contains(k) || d.TenNguoiNhan.Contains(k) || d.DienThoai.Contains(k));
        }
        if (tuNgay.HasValue) q = q.Where(d => d.NgayDat >= tuNgay.Value.Date);
        if (denNgay.HasValue)
        {
            var den = denNgay.Value.Date.AddDays(1);
            q = q.Where(d => d.NgayDat < den);
        }

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

        var dem = await _db.DonHangs.AsNoTracking().GroupBy(d => d.TrangThai)
            .Select(g => new { g.Key, Dem = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Dem);

        return View(new DonHangListVM
        {
            Rows = rows, TrangThai = trangThai, TuKhoa = tuKhoa, TuNgay = tuNgay, DenNgay = denNgay,
            Trang = trang, TongTrang = tongTrang, TongSo = tong, DemTheoTrangThai = dem
        });
    }

    // ================= CHI TIẾT + IN PHIẾU =================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ChiTiet(int id)
    {
        var vm = await LayChiTietAsync(id);
        return vm == null ? NotFound() : View(vm);
    }

    [HttpGet("{id:int}/in")]
    public async Task<IActionResult> In(int id)
    {
        var vm = await LayChiTietAsync(id);
        return vm == null ? NotFound() : View(vm);
    }

    // ================= THAO TÁC =================
    [HttpPost("{id:int}/chuyen"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Chuyen(int id, string trangThaiMoi)
    {
        var kq = await _don.DoiTrangThaiAsync(id, trangThaiMoi, UserId);
        TempData[kq.Ok ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost("{id:int}/huy"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id)
    {
        var kq = await _don.HuyAsync(id, UserId, laNhanVien: true);
        TempData[kq.Ok ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost("{id:int}/thanh-toan"), ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanThanhToan(int id)
    {
        var kq = await _don.XacNhanThanhToanAsync(id, UserId);
        TempData[kq.Ok ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    private async Task<DonHangChiTietVM?> LayChiTietAsync(int id)
    {
        var vm = await _db.DonHangs.AsNoTracking().Where(d => d.Id == id)
            .Select(d => new DonHangChiTietVM
            {
                Id = d.Id, MaDon = d.MaDon, NgayDat = d.NgayDat, TrangThai = d.TrangThai,
                PhuongThucThanhToan = d.PhuongThucThanhToan, TrangThaiThanhToan = d.TrangThaiThanhToan,
                TenNguoiNhan = d.TenNguoiNhan, DienThoai = d.DienThoai, DiaChiGiao = d.DiaChiGiao, GhiChu = d.GhiChu,
                PhiVanChuyen = d.PhiVanChuyen, TienGiam = d.TienGiam, TongTien = d.TongTien,
                MaGiamGia = d.MaGiamGia != null ? d.MaGiamGia.Ma : null,
                EmailKhach = d.NguoiDung != null ? d.NguoiDung.Email : null
            }).FirstOrDefaultAsync();
        if (vm == null) return null;

        vm.Dong = await _db.ChiTietDonHangs.AsNoTracking().Where(c => c.DonHangId == id).OrderBy(c => c.Id)
            .Select(c => new DonHangDongVM
            {
                SanPhamId = c.SanPhamId, MaSP = c.SanPham!.MaSP, TenSP = c.SanPham.TenSP, Slug = c.SanPham.Slug,
                HinhAnh = c.SanPham.HinhAnh, SoLuong = c.SoLuong, DonGia = c.DonGia
            }).ToListAsync();
        return vm;
    }
}
