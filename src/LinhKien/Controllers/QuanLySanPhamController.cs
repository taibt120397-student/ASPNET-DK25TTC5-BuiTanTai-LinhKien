using LinhKien.Data;
using LinhKien.Helpers;
using LinhKien.Models;
using LinhKien.Services;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Controllers;

[Authorize(Roles = "Admin,NhanVien")]
[Route("quan-tri/san-pham")]
public class QuanLySanPhamController : Controller
{
    private const int PageSize = 10;

    private readonly AppDbContext _db;
    private readonly IImageService _img;

    public QuanLySanPhamController(AppDbContext db, IImageService img)
    {
        _db = db;
        _img = img;
    }

    // ================= DANH SÁCH =================
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, int? danhMucId, int? thuongHieuId, int trang = 1)
    {
        var allDm = await _db.DanhMucs.AsNoTracking().ToListAsync();
        var query = _db.SanPhams.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(s => s.MaSP.Contains(q) || s.TenSP.Contains(q));
        }
        if (danhMucId.HasValue)
        {
            var ids = DanhMucTree.SubtreeIds(allDm, danhMucId.Value); // gồm cả danh mục con
            query = query.Where(s => ids.Contains(s.DanhMucId));
        }
        if (thuongHieuId.HasValue)
            query = query.Where(s => s.ThuongHieuId == thuongHieuId.Value);

        var tong = await query.CountAsync();
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tong / (double)PageSize));
        trang = Math.Clamp(trang, 1, tongTrang);

        var rows = await (from s in query
                          join d in _db.DanhMucs on s.DanhMucId equals d.Id
                          join t in _db.ThuongHieus on s.ThuongHieuId equals t.Id
                          orderby s.MaSP
                          select new SanPhamRow
                          {
                              Id = s.Id,
                              MaSP = s.MaSP,
                              TenSP = s.TenSP,
                              DanhMuc = d.TenDanhMuc,
                              ThuongHieu = t.TenThuongHieu,
                              Gia = s.Gia,
                              GiaKhuyenMai = s.GiaKhuyenMai,
                              SoLuongTon = s.SoLuongTon,
                              MucTonToiThieu = s.MucTonToiThieu,
                              NgungBan = s.NgungBan,
                              HinhAnh = s.HinhAnh
                          })
            .Skip((trang - 1) * PageSize).Take(PageSize)
            .ToListAsync();

        ViewBag.DanhMucList = DanhMucTree.Options(allDm);
        ViewBag.ThuongHieuList = await ThuongHieuOptionsAsync();

        return View(new SanPhamIndexVM
        {
            Rows = rows, Q = q, DanhMucId = danhMucId, ThuongHieuId = thuongHieuId,
            Trang = trang, TongTrang = tongTrang, TongSo = tong
        });
    }

    // ================= THÊM =================
    [HttpGet("tao")]
    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View("Form", new SanPhamFormVM { MaSP = await NextMaSPAsync() });
    }

    [HttpPost("tao"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SanPhamFormVM vm)
    {
        await ValidateAsync(vm);
        if (!ModelState.IsValid)
        {
            vm.MaSP = await NextMaSPAsync();
            await LoadListsAsync();
            return View("Form", vm);
        }

        string? hinh = null;
        if (vm.Anh is { Length: > 0 }) hinh = await _img.SaveProductImageAsync(vm.Anh);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var sp = new SanPham
            {
                MaSP = await NextMaSPAsync(), // luôn sinh ở server, không tin dữ liệu form
                TenSP = vm.TenSP.Trim(),
                Slug = await UniqueSlugAsync(vm.Slug, vm.TenSP, 0),
                MoTa = vm.MoTa,
                Gia = vm.Gia,
                GiaKhuyenMai = vm.GiaKhuyenMai,
                SoLuongTon = vm.SoLuongTon,
                MucTonToiThieu = vm.MucTonToiThieu,
                DanhMucId = vm.DanhMucId!.Value,
                ThuongHieuId = vm.ThuongHieuId!.Value,
                HinhAnh = hinh,
                DatasheetUrl = string.IsNullOrWhiteSpace(vm.DatasheetUrl) ? null : vm.DatasheetUrl.Trim(),
                NgungBan = vm.NgungBan
            };
            _db.SanPhams.Add(sp);
            await _db.SaveChangesAsync();

            foreach (var t in CleanSpecs(vm))
                _db.ThongSos.Add(new ThongSoKyThuat { SanPhamId = sp.Id, TenThongSo = t.Ten, GiaTri = t.GiaTri, DonVi = t.DonVi });
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            TempData["ThongBao"] = $"Đã thêm sản phẩm {sp.MaSP} – {sp.TenSP}.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            _img.Delete(hinh);
            ModelState.AddModelError("", "Không lưu được sản phẩm (có thể trùng mã/slug). Vui lòng thử lại.");
            vm.MaSP = await NextMaSPAsync();
            await LoadListsAsync();
            return View("Form", vm);
        }
    }

    // ================= SỬA =================
    [HttpGet("{id:int}/sua")]
    public async Task<IActionResult> Edit(int id)
    {
        var sp = await _db.SanPhams.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (sp == null) return NotFound();

        var specs = await _db.ThongSos.AsNoTracking()
            .Where(t => t.SanPhamId == id).OrderBy(t => t.Id).ToListAsync();

        await LoadListsAsync();
        return View("Form", new SanPhamFormVM
        {
            Id = sp.Id, MaSP = sp.MaSP, TenSP = sp.TenSP, Slug = sp.Slug, MoTa = sp.MoTa,
            Gia = sp.Gia, GiaKhuyenMai = sp.GiaKhuyenMai,
            SoLuongTon = sp.SoLuongTon, MucTonToiThieu = sp.MucTonToiThieu,
            DanhMucId = sp.DanhMucId, ThuongHieuId = sp.ThuongHieuId,
            DatasheetUrl = sp.DatasheetUrl, NgungBan = sp.NgungBan,
            HinhAnh = sp.HinhAnh, RowVersion = sp.RowVersion,
            ThongSo = specs.Select(t => new ThongSoVM { TenThongSo = t.TenThongSo, GiaTri = t.GiaTri, DonVi = t.DonVi }).ToList()
        });
    }

    [HttpPost("{id:int}/sua"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SanPhamFormVM vm)
    {
        vm.Id = id;
        var sp = await _db.SanPhams.FirstOrDefaultAsync(s => s.Id == id);
        if (sp == null) return NotFound();

        vm.MaSP = sp.MaSP;
        vm.HinhAnh = sp.HinhAnh;

        await ValidateAsync(vm);
        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View("Form", vm);
        }

        // Kiểm soát đồng thời: so RowVersion lúc mở form với RowVersion hiện tại
        if (vm.RowVersion != null)
            _db.Entry(sp).Property(s => s.RowVersion).OriginalValue = vm.RowVersion;

        string? hinhMoi = null;
        var hinhCu = sp.HinhAnh;
        if (vm.Anh is { Length: > 0 }) hinhMoi = await _img.SaveProductImageAsync(vm.Anh);

        sp.TenSP = vm.TenSP.Trim();
        sp.Slug = string.IsNullOrWhiteSpace(vm.Slug) && !string.IsNullOrEmpty(sp.Slug)
            ? sp.Slug
            : await UniqueSlugAsync(vm.Slug, vm.TenSP, id);
        sp.MoTa = vm.MoTa;
        sp.Gia = vm.Gia;
        sp.GiaKhuyenMai = vm.GiaKhuyenMai;
        sp.SoLuongTon = vm.SoLuongTon;
        sp.MucTonToiThieu = vm.MucTonToiThieu;
        sp.DanhMucId = vm.DanhMucId!.Value;
        sp.ThuongHieuId = vm.ThuongHieuId!.Value;
        sp.DatasheetUrl = string.IsNullOrWhiteSpace(vm.DatasheetUrl) ? null : vm.DatasheetUrl.Trim();
        sp.NgungBan = vm.NgungBan;
        if (hinhMoi != null) sp.HinhAnh = hinhMoi;

        // Thông số: thay toàn bộ bằng danh sách mới
        _db.ThongSos.RemoveRange(await _db.ThongSos.Where(t => t.SanPhamId == id).ToListAsync());
        foreach (var t in CleanSpecs(vm))
            _db.ThongSos.Add(new ThongSoKyThuat { SanPhamId = id, TenThongSo = t.Ten, GiaTri = t.GiaTri, DonVi = t.DonVi });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _img.Delete(hinhMoi);
            ModelState.AddModelError("", "Sản phẩm vừa được người khác cập nhật. Hãy tải lại trang (F5) để xem dữ liệu mới rồi sửa lại.");
            await LoadListsAsync();
            return View("Form", vm);
        }
        catch (DbUpdateException)
        {
            _img.Delete(hinhMoi);
            ModelState.AddModelError("", "Không lưu được sản phẩm (có thể trùng slug). Vui lòng thử lại.");
            await LoadListsAsync();
            return View("Form", vm);
        }

        if (hinhMoi != null) _img.Delete(hinhCu); // chỉ xóa ảnh cũ khi đã lưu thành công
        TempData["ThongBao"] = $"Đã cập nhật sản phẩm {sp.MaSP}.";
        return RedirectToAction(nameof(Index));
    }

    // ================= ẨN/HIỆN, XÓA =================
    [HttpPost("{id:int}/an-hien"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, string? returnUrl)
    {
        var sp = await _db.SanPhams.FirstOrDefaultAsync(s => s.Id == id);
        if (sp == null) return NotFound();

        sp.NgungBan = !sp.NgungBan;
        await _db.SaveChangesAsync();
        TempData["ThongBao"] = sp.NgungBan ? $"Đã ẩn {sp.MaSP}." : $"Đã hiển thị lại {sp.MaSP}.";
        return Back(returnUrl);
    }

    [HttpPost("{id:int}/xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var sp = await _db.SanPhams.FirstOrDefaultAsync(s => s.Id == id);
        if (sp == null) return NotFound();

        if (await _db.ChiTietDonHangs.AnyAsync(c => c.SanPhamId == id))
        {
            TempData["Loi"] = $"{sp.MaSP} đã có trong đơn hàng nên không thể xóa (để giữ lịch sử đơn). Hãy dùng “Ẩn”.";
            return Back(returnUrl);
        }

        try
        {
            _db.ThongSos.RemoveRange(_db.ThongSos.Where(t => t.SanPhamId == id));
            _db.ChiTietGioHangs.RemoveRange(_db.ChiTietGioHangs.Where(c => c.SanPhamId == id));
            _db.SanPhams.Remove(sp);
            await _db.SaveChangesAsync();
            _img.Delete(sp.HinhAnh);
            TempData["ThongBao"] = $"Đã xóa sản phẩm {sp.MaSP}.";
        }
        catch (DbUpdateException)
        {
            TempData["Loi"] = $"Không thể xóa {sp.MaSP} vì còn dữ liệu liên quan (vd đánh giá). Hãy dùng “Ẩn”.";
        }
        return Back(returnUrl);
    }

    // ================= HỖ TRỢ =================
    private IActionResult Back(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));

    private async Task LoadListsAsync()
    {
        var all = await _db.DanhMucs.AsNoTracking().ToListAsync();
        ViewBag.DanhMucList = DanhMucTree.Options(all);
        ViewBag.ThuongHieuList = await ThuongHieuOptionsAsync();
    }

    private async Task<List<SelectListItem>> ThuongHieuOptionsAsync() =>
        await _db.ThuongHieus.AsNoTracking()
            .OrderBy(t => t.TenThuongHieu)
            .Select(t => new SelectListItem(t.TenThuongHieu, t.Id.ToString()))
            .ToListAsync();

    /// <summary>LK0001, LK0002... = số lớn nhất hiện có + 1.</summary>
    private async Task<string> NextMaSPAsync()
    {
        var codes = await _db.SanPhams.Where(s => s.MaSP.StartsWith("LK")).Select(s => s.MaSP).ToListAsync();
        var max = 0;
        foreach (var c in codes)
            if (int.TryParse(c.AsSpan(2), out var n) && n > max) max = n;
        return "LK" + (max + 1).ToString("D4");
    }

    private async Task<string> UniqueSlugAsync(string? slugInput, string ten, int selfId)
    {
        var baseSlug = SlugHelper.ToSlug(string.IsNullOrWhiteSpace(slugInput) ? ten : slugInput);
        if (baseSlug == "") baseSlug = "san-pham";
        var slug = baseSlug;
        var i = 2;
        while (await _db.SanPhams.AnyAsync(s => s.Slug == slug && s.Id != selfId))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    private static List<(string Ten, string GiaTri, string? DonVi)> CleanSpecs(SanPhamFormVM vm) =>
        vm.ThongSo
            .Where(t => !string.IsNullOrWhiteSpace(t.TenThongSo) && !string.IsNullOrWhiteSpace(t.GiaTri))
            .Select(t => (t.TenThongSo!.Trim(), t.GiaTri!.Trim(), string.IsNullOrWhiteSpace(t.DonVi) ? null : t.DonVi.Trim()))
            .ToList();

    private async Task ValidateAsync(SanPhamFormVM vm)
    {
        if (vm.GiaKhuyenMai.HasValue && vm.GiaKhuyenMai.Value >= vm.Gia)
            ModelState.AddModelError(nameof(vm.GiaKhuyenMai), "Giá khuyến mãi phải nhỏ hơn giá gốc.");

        if (vm.DanhMucId.HasValue && !await _db.DanhMucs.AnyAsync(d => d.Id == vm.DanhMucId.Value))
            ModelState.AddModelError(nameof(vm.DanhMucId), "Danh mục không tồn tại.");
        if (vm.ThuongHieuId.HasValue && !await _db.ThuongHieus.AnyAsync(t => t.Id == vm.ThuongHieuId.Value))
            ModelState.AddModelError(nameof(vm.ThuongHieuId), "Thương hiệu không tồn tại.");

        if (vm.Anh is { Length: > 0 })
        {
            var loi = _img.Validate(vm.Anh);
            if (loi != null) ModelState.AddModelError(nameof(vm.Anh), loi);
        }

        // Chỉ cho http/https, chặn javascript: và các scheme nguy hiểm
        if (!string.IsNullOrWhiteSpace(vm.DatasheetUrl))
        {
            var ok = Uri.TryCreate(vm.DatasheetUrl.Trim(), UriKind.Absolute, out var u)
                     && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
            if (!ok) ModelState.AddModelError(nameof(vm.DatasheetUrl), "Liên kết datasheet phải bắt đầu bằng http:// hoặc https://");
        }

        for (var i = 0; i < vm.ThongSo.Count; i++)
        {
            var t = vm.ThongSo[i];
            var coTen = !string.IsNullOrWhiteSpace(t.TenThongSo);
            var coGt = !string.IsNullOrWhiteSpace(t.GiaTri);
            if (coTen != coGt)
                ModelState.AddModelError("", $"Dòng thông số {i + 1}: cần nhập cả tên thông số và giá trị.");
        }
    }
}
