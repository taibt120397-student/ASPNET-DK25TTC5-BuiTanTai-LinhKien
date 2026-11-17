using System.Diagnostics;
using LinhKien.Data;
using LinhKien.Helpers;
using LinhKien.Models;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var all = await _db.DanhMucs.AsNoTracking().ToListAsync();
        var nhom = all.Where(d => d.DanhMucChaId == null)
            .OrderBy(d => d.ThuTu).ThenBy(d => d.TenDanhMuc)
            .Select(cha => new DanhMucNhomVM
            {
                Ten = cha.TenDanhMuc,
                Slug = cha.Slug,
                Con = all.Where(c => c.DanhMucChaId == cha.Id)
                         .OrderBy(c => c.ThuTu).ThenBy(c => c.TenDanhMuc)
                         .Select(c => (c.TenDanhMuc, (string?)c.Slug)).ToList()
            }).ToList();

        var khuyenMai = await _db.SanPhams.AsNoTracking()
            .Where(s => !s.NgungBan && s.GiaKhuyenMai != null)
            .OrderByDescending(s => s.Id).Take(8)
            .Select(SanPhamProjection.Card).ToListAsync();

        var moi = await _db.SanPhams.AsNoTracking()
            .Where(s => !s.NgungBan)
            .OrderByDescending(s => s.Id).Take(8)
            .Select(SanPhamProjection.Card).ToListAsync();

        await SanPhamProjection.GanThuongHieuAsync(_db, khuyenMai, moi);

        return View(new TrangChuVM { DanhMuc = nhom, KhuyenMai = khuyenMai, Moi = moi });
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
