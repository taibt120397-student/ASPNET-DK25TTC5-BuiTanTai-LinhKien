using LinhKien.Data;
using LinhKien.Helpers;
using LinhKien.Models;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Controllers;

[Authorize(Roles = "Admin,NhanVien")]
[Route("quan-tri/danh-muc")]
public class QuanLyDanhMucController : Controller
{
    private readonly AppDbContext _db;

    public QuanLyDanhMucController(AppDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var all = await _db.DanhMucs.AsNoTracking().ToListAsync();
        var soSp = await _db.SanPhams.AsNoTracking()
            .GroupBy(s => s.DanhMucId)
            .Select(g => new { g.Key, Dem = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Dem);
        var soCon = all.Where(d => d.DanhMucChaId != null)
            .GroupBy(d => d.DanhMucChaId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var rows = DanhMucTree.Flatten(all).Select(x => new DanhMucRow
        {
            Id = x.Node.Id,
            Ten = x.Node.TenDanhMuc,
            Slug = x.Node.Slug,
            Level = x.Level,
            ThuTu = x.Node.ThuTu,
            SoCon = soCon.GetValueOrDefault(x.Node.Id),
            SoSanPham = soSp.GetValueOrDefault(x.Node.Id)
        }).ToList();

        return View(rows);
    }

    [HttpGet("tao")]
    public async Task<IActionResult> Create()
    {
        await LoadParentsAsync(null);
        return View("Form", new DanhMucFormVM());
    }

    [HttpPost("tao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DanhMucFormVM vm)
    {
        await ValidateParentAsync(vm, null);
        if (!ModelState.IsValid)
        {
            await LoadParentsAsync(null);
            return View("Form", vm);
        }

        var dm = new DanhMuc
        {
            TenDanhMuc = vm.TenDanhMuc.Trim(),
            Slug = await UniqueSlugAsync(vm.Slug, vm.TenDanhMuc, 0),
            DanhMucChaId = vm.DanhMucChaId,
            ThuTu = vm.ThuTu
        };
        _db.DanhMucs.Add(dm);
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = $"Đã thêm danh mục “{dm.TenDanhMuc}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var dm = await _db.DanhMucs.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (dm == null) return NotFound();

        await LoadParentsAsync(id);
        return View("Form", new DanhMucFormVM
        {
            Id = dm.Id,
            TenDanhMuc = dm.TenDanhMuc,
            Slug = dm.Slug,
            DanhMucChaId = dm.DanhMucChaId,
            ThuTu = dm.ThuTu
        });
    }

    [HttpPost("{id:int}/sua"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DanhMucFormVM vm)
    {
        vm.Id = id;
        var dm = await _db.DanhMucs.FirstOrDefaultAsync(d => d.Id == id);
        if (dm == null) return NotFound();

        await ValidateParentAsync(vm, id);
        if (!ModelState.IsValid)
        {
            await LoadParentsAsync(id);
            return View("Form", vm);
        }

        dm.TenDanhMuc = vm.TenDanhMuc.Trim();
        dm.Slug = string.IsNullOrWhiteSpace(vm.Slug) ? dm.Slug : await UniqueSlugAsync(vm.Slug, vm.TenDanhMuc, id);
        dm.DanhMucChaId = vm.DanhMucChaId;
        dm.ThuTu = vm.ThuTu;
        await _db.SaveChangesAsync();

        TempData["ThongBao"] = $"Đã cập nhật danh mục “{dm.TenDanhMuc}”.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var dm = await _db.DanhMucs.FirstOrDefaultAsync(d => d.Id == id);
        if (dm == null) return NotFound();

        if (await _db.DanhMucs.AnyAsync(d => d.DanhMucChaId == id))
            TempData["Loi"] = "Danh mục còn danh mục con, hãy xóa hoặc chuyển danh mục con trước.";
        else if (await _db.SanPhams.AnyAsync(s => s.DanhMucId == id))
            TempData["Loi"] = "Danh mục còn sản phẩm, hãy chuyển sản phẩm sang danh mục khác trước.";
        else
        {
            _db.DanhMucs.Remove(dm);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa danh mục “{dm.TenDanhMuc}”.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------- hỗ trợ ----------

    private async Task LoadParentsAsync(int? excludeId)
    {
        var all = await _db.DanhMucs.AsNoTracking().ToListAsync();
        ViewBag.ParentList = DanhMucTree.Options(all, excludeId);
    }

    /// <summary>Chặn chọn cha là chính nó hoặc con cháu của nó (sẽ tạo vòng lặp).</summary>
    private async Task ValidateParentAsync(DanhMucFormVM vm, int? selfId)
    {
        if (!vm.DanhMucChaId.HasValue) return;
        var all = await _db.DanhMucs.AsNoTracking().ToListAsync();
        var allowed = DanhMucTree.Flatten(all, selfId).Select(x => x.Node.Id).ToHashSet();
        if (!allowed.Contains(vm.DanhMucChaId.Value))
            ModelState.AddModelError(nameof(vm.DanhMucChaId), "Danh mục cha không hợp lệ.");
    }

    private async Task<string> UniqueSlugAsync(string? slugInput, string ten, int selfId)
    {
        var baseSlug = SlugHelper.ToSlug(string.IsNullOrWhiteSpace(slugInput) ? ten : slugInput);
        if (baseSlug == "") baseSlug = "danh-muc";
        var slug = baseSlug;
        var i = 2;
        while (await _db.DanhMucs.AnyAsync(d => d.Slug == slug && d.Id != selfId))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }
}
