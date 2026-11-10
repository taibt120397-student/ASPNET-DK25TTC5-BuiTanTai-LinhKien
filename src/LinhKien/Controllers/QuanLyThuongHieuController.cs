using LinhKien.Data;
using LinhKien.Models;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Controllers;

[Authorize(Roles = "Admin,NhanVien")]
[Route("quan-tri/thuong-hieu")]
public class QuanLyThuongHieuController : Controller
{
    private readonly AppDbContext _db;

    public QuanLyThuongHieuController(AppDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var soSp = await _db.SanPhams.AsNoTracking()
            .GroupBy(s => s.ThuongHieuId)
            .Select(g => new { g.Key, Dem = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Dem);

        var list = await _db.ThuongHieus.AsNoTracking().OrderBy(t => t.TenThuongHieu).ToListAsync();
        var rows = list.Select(t => new ThuongHieuRow
        {
            Id = t.Id,
            Ten = t.TenThuongHieu,
            QuocGia = t.QuocGia,
            SoSanPham = soSp.GetValueOrDefault(t.Id)
        }).ToList();
        return View(rows);
    }

    [HttpGet("tao")]
    public IActionResult Create() => View("Form", new ThuongHieuFormVM());

    [HttpPost("tao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThuongHieuFormVM vm)
    {
        await CheckDuplicateAsync(vm, 0);
        if (!ModelState.IsValid) return View("Form", vm);

        var th = new ThuongHieu { TenThuongHieu = vm.TenThuongHieu.Trim(), QuocGia = vm.QuocGia?.Trim() };
        _db.ThuongHieus.Add(th);
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = $"Đã thêm thương hiệu “{th.TenThuongHieu}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var th = await _db.ThuongHieus.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (th == null) return NotFound();
        return View("Form", new ThuongHieuFormVM { Id = th.Id, TenThuongHieu = th.TenThuongHieu, QuocGia = th.QuocGia });
    }

    [HttpPost("{id:int}/sua"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ThuongHieuFormVM vm)
    {
        vm.Id = id;
        var th = await _db.ThuongHieus.FirstOrDefaultAsync(t => t.Id == id);
        if (th == null) return NotFound();

        await CheckDuplicateAsync(vm, id);
        if (!ModelState.IsValid) return View("Form", vm);

        th.TenThuongHieu = vm.TenThuongHieu.Trim();
        th.QuocGia = vm.QuocGia?.Trim();
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = $"Đã cập nhật thương hiệu “{th.TenThuongHieu}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var th = await _db.ThuongHieus.FirstOrDefaultAsync(t => t.Id == id);
        if (th == null) return NotFound();

        if (await _db.SanPhams.AnyAsync(s => s.ThuongHieuId == id))
            TempData["Loi"] = "Thương hiệu còn sản phẩm, không thể xóa.";
        else
        {
            _db.ThuongHieus.Remove(th);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa thương hiệu “{th.TenThuongHieu}”.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task CheckDuplicateAsync(ThuongHieuFormVM vm, int selfId)
    {
        var ten = vm.TenThuongHieu?.Trim() ?? "";
        if (ten != "" && await _db.ThuongHieus.AnyAsync(t => t.TenThuongHieu == ten && t.Id != selfId))
            ModelState.AddModelError(nameof(vm.TenThuongHieu), "Thương hiệu này đã tồn tại.");
    }
}
