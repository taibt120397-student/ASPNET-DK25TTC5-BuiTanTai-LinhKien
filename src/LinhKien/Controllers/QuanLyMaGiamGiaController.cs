using LinhKien.Data;
using LinhKien.Models;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Controllers;

/// <summary>Quản lý mã giảm giá: chỉ Admin (UC12).</summary>
[Authorize(Roles = "Admin")]
[Route("quan-tri/ma-giam-gia")]
public class QuanLyMaGiamGiaController : Controller
{
    private readonly AppDbContext _db;

    public QuanLyMaGiamGiaController(AppDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var rows = await _db.MaGiamGias.AsNoTracking().OrderByDescending(m => m.Id)
            .Select(m => new MaGiamGiaRowVM
            {
                Id = m.Id, Ma = m.Ma, LaPhanTram = m.LaPhanTram, GiaTriGiam = m.GiaTriGiam, DonToiThieu = m.DonToiThieu,
                NgayBatDau = m.NgayBatDau, NgayKetThuc = m.NgayKetThuc, SoLanToiDa = m.SoLanToiDa,
                SoLanDaDung = m.SoLanDaDung, KichHoat = m.KichHoat
            }).ToListAsync();
        return View(rows);
    }

    [HttpGet("tao")]
    public IActionResult Create() =>
        View("Form", new MaGiamGiaFormVM { NgayBatDau = DateTime.Today, NgayKetThuc = DateTime.Today.AddMonths(1) });

    [HttpPost("tao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaGiamGiaFormVM vm)
    {
        await KiemTraAsync(vm, 0);
        if (!ModelState.IsValid) return View("Form", vm);

        var m = new MaGiamGia();
        GanDuLieu(m, vm);
        _db.MaGiamGias.Add(m);
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = $"Đã tạo mã giảm giá {m.Ma}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var m = await _db.MaGiamGias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        return View("Form", new MaGiamGiaFormVM
        {
            Id = m.Id, Ma = m.Ma, LaPhanTram = m.LaPhanTram, GiaTriGiam = m.GiaTriGiam, DonToiThieu = m.DonToiThieu,
            NgayBatDau = m.NgayBatDau.Date, NgayKetThuc = m.NgayKetThuc.Date, SoLanToiDa = m.SoLanToiDa, KichHoat = m.KichHoat
        });
    }

    [HttpPost("{id:int}/sua"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MaGiamGiaFormVM vm)
    {
        vm.Id = id;
        var m = await _db.MaGiamGias.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();

        await KiemTraAsync(vm, id);
        if (vm.SoLanToiDa < m.SoLanDaDung)
            ModelState.AddModelError(nameof(vm.SoLanToiDa), $"Mã đã được dùng {m.SoLanDaDung} lần, số lần tối đa không được nhỏ hơn.");
        if (!ModelState.IsValid) return View("Form", vm);

        GanDuLieu(m, vm);
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = $"Đã cập nhật mã giảm giá {m.Ma}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Khóa hoặc mở khóa mã (mã đã khóa không áp dụng được cho đơn mới).</summary>
    [HttpPost("{id:int}/khoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Khoa(int id)
    {
        var m = await _db.MaGiamGias.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        m.KichHoat = !m.KichHoat;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = m.KichHoat ? $"Đã mở khóa mã {m.Ma}." : $"Đã khóa mã {m.Ma}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await _db.MaGiamGias.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();

        if (m.SoLanDaDung > 0 || await _db.DonHangs.AnyAsync(d => d.MaGiamGiaId == id))
        {
            TempData["Loi"] = $"Mã {m.Ma} đã được dùng trong đơn hàng nên không thể xóa. Hãy dùng “Khóa”.";
            return RedirectToAction(nameof(Index));
        }
        _db.MaGiamGias.Remove(m);
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = $"Đã xóa mã {m.Ma}.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- hỗ trợ ----------
    private async Task KiemTraAsync(MaGiamGiaFormVM vm, int selfId)
    {
        var ma = vm.Ma?.Trim() ?? "";
        if (ma != "" && await _db.MaGiamGias.AnyAsync(x => x.Ma == ma && x.Id != selfId))
            ModelState.AddModelError(nameof(vm.Ma), "Mã giảm giá này đã tồn tại.");

        if (vm.LaPhanTram && (vm.GiaTriGiam < 1 || vm.GiaTriGiam > 100))
            ModelState.AddModelError(nameof(vm.GiaTriGiam), "Giảm theo phần trăm phải từ 1 đến 100.");

        if (vm.NgayBatDau.HasValue && vm.NgayKetThuc.HasValue && vm.NgayKetThuc.Value.Date < vm.NgayBatDau.Value.Date)
            ModelState.AddModelError(nameof(vm.NgayKetThuc), "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
    }

    private static void GanDuLieu(MaGiamGia m, MaGiamGiaFormVM vm)
    {
        m.Ma = vm.Ma.Trim().ToUpperInvariant();
        m.LaPhanTram = vm.LaPhanTram;
        m.GiaTriGiam = vm.GiaTriGiam;
        m.DonToiThieu = vm.DonToiThieu;
        m.NgayBatDau = vm.NgayBatDau!.Value.Date;                         // từ 00:00 ngày bắt đầu
        m.NgayKetThuc = vm.NgayKetThuc!.Value.Date.AddDays(1).AddSeconds(-1); // đến hết ngày kết thúc
        m.SoLanToiDa = vm.SoLanToiDa;
        m.KichHoat = vm.KichHoat;
    }
}
