using LinhKien.Data;
using LinhKien.Helpers;
using LinhKien.Models;
using LinhKien.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Controllers;

/// <summary>Khu vực dành cho khách: danh sách, tìm kiếm, lọc, chi tiết. Không cần đăng nhập.</summary>
[Route("san-pham")]
public class SanPhamController : Controller
{
    private const int PageSize = 12;

    private readonly AppDbContext _db;

    public SanPhamController(AppDbContext db) => _db = db;

    // ================= DANH SÁCH + TÌM KIẾM + LỌC =================
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? q, string? danhMuc, int? thuongHieu, decimal? giaTu, decimal? giaDen,
        bool conHang = false, string sapXep = "moi", string[]? ts = null, int trang = 1)
    {
        var allDm = await _db.DanhMucs.AsNoTracking().ToListAsync();
        var thongSo = _db.Set<ThongSoKyThuat>().AsNoTracking();

        // Khách chỉ thấy sản phẩm đang bán
        var query = _db.SanPhams.AsNoTracking().Where(s => !s.NgungBan);

        // 1. Danh mục (gồm cả danh mục con)
        DanhMuc? dmHienTai = null;
        if (!string.IsNullOrWhiteSpace(danhMuc))
        {
            dmHienTai = allDm.FirstOrDefault(d => d.Slug == danhMuc);
            if (dmHienTai == null) return NotFound();
            var ids = DanhMucTree.SubtreeIds(allDm, dmHienTai.Id);
            query = query.Where(s => ids.Contains(s.DanhMucId));
        }

        // 2. Từ khóa: mỗi từ phải khớp tên hoặc mã
        foreach (var t in TachTuKhoa(q))
            query = query.Where(s => s.TenSP.Contains(t) || s.MaSP.Contains(t));

        // Tập "nền" (danh mục + từ khóa) dùng để đếm các lựa chọn lọc
        var nen = query;

        // 3. Dữ liệu cho bộ lọc thương hiệu
        var demHang = await nen.GroupBy(s => s.ThuongHieuId)
            .Select(g => new { Id = g.Key, Dem = g.Count() }).ToListAsync();
        var idHang = demHang.Select(x => x.Id).ToList();
        var tenHang = await _db.ThuongHieus.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.TenThuongHieu);
        var hangFacet = demHang
            .Select(x => new ThuongHieuFacetVM { Id = x.Id, Ten = tenHang.GetValueOrDefault(x.Id, "?"), Dem = x.Dem })
            .OrderBy(x => x.Ten).ToList();

        // 4. Dữ liệu cho bộ lọc thông số kỹ thuật
        var tsChon = (ts ?? Array.Empty<string>()).Where(x => x.Contains('|')).Distinct().ToList();
        var nenIds = nen.Select(s => s.Id);
        var tsRows = await thongSo
            .Where(t => nenIds.Contains(t.SanPhamId))
            .GroupBy(t => new { t.TenThongSo, t.GiaTri, t.DonVi })
            .Select(g => new { g.Key.TenThongSo, g.Key.GiaTri, g.Key.DonVi, Dem = g.Count() })
            .ToListAsync();

        // Chỉ giữ thông số có ít giá trị (2–12) để làm bộ lọc có ích; lấy tối đa 5 thông số phổ biến nhất
        var facets = tsRows.GroupBy(r => r.TenThongSo)
            .Where(g => g.Count() >= 2 && g.Count() <= 12 && g.Sum(x => x.Dem) >= 3)
            .OrderByDescending(g => g.Sum(x => x.Dem)).Take(5)
            .Select(g => new FacetThongSoVM
            {
                Ten = g.Key,
                GiaTri = g.OrderBy(x => x.GiaTri).Select(x => new FacetGiaTriVM
                {
                    GiaTri = x.GiaTri, DonVi = x.DonVi, Dem = x.Dem,
                    Chon = tsChon.Contains($"{g.Key}|{x.GiaTri}")
                }).ToList()
            }).ToList();

        // 5. Áp bộ lọc
        if (thuongHieu.HasValue)
            query = query.Where(s => s.ThuongHieuId == thuongHieu.Value);
        if (giaTu.HasValue)
            query = query.Where(s => (s.GiaKhuyenMai ?? s.Gia) >= giaTu.Value);
        if (giaDen.HasValue)
            query = query.Where(s => (s.GiaKhuyenMai ?? s.Gia) <= giaDen.Value);
        if (conHang)
            query = query.Where(s => s.SoLuongTon > 0);

        // Thông số: các giá trị cùng tên là HOẶC, khác tên là VÀ
        foreach (var nhom in tsChon.GroupBy(x => x[..x.IndexOf('|')]))
        {
            var ten = nhom.Key;
            var giaTris = nhom.Select(x => x[(x.IndexOf('|') + 1)..]).ToList();
            query = query.Where(s => thongSo.Any(t => t.SanPhamId == s.Id && t.TenThongSo == ten && giaTris.Contains(t.GiaTri)));
        }

        // 6. Sắp xếp
        sapXep = sapXep is "gia-tang" or "gia-giam" or "ten" ? sapXep : "moi";
        query = sapXep switch
        {
            "gia-tang" => query.OrderBy(s => s.GiaKhuyenMai ?? s.Gia).ThenBy(s => s.Id),
            "gia-giam" => query.OrderByDescending(s => s.GiaKhuyenMai ?? s.Gia).ThenBy(s => s.Id),
            "ten" => query.OrderBy(s => s.TenSP),
            _ => query.OrderByDescending(s => s.Id)
        };

        // 7. Phân trang ở database
        var tong = await query.CountAsync();
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tong / (double)PageSize));
        trang = Math.Clamp(trang, 1, tongTrang);

        var items = await query.Skip((trang - 1) * PageSize).Take(PageSize)
            .Select(SanPhamProjection.Card).ToListAsync();
        foreach (var c in items) c.ThuongHieuTen = tenHang.GetValueOrDefault(c.ThuongHieuId);

        // 8. Thanh bên danh mục + breadcrumb
        var byId = allDm.ToDictionary(d => d.Id);
        var crumbs = new List<(string Ten, string? Slug)>();
        var cur = dmHienTai;
        while (cur != null)
        {
            crumbs.Insert(0, (cur.TenDanhMuc, cur.Slug));
            cur = cur.DanhMucChaId.HasValue && byId.TryGetValue(cur.DanhMucChaId.Value, out var cha) ? cha : (DanhMuc?)null;
        }

        return View(new SanPhamListVM
        {
            Items = items,
            Q = q,
            DanhMuc = danhMuc,
            TenDanhMuc = dmHienTai?.TenDanhMuc,
            ThuongHieu = thuongHieu,
            GiaTu = giaTu,
            GiaDen = giaDen,
            ConHang = conHang,
            SapXep = sapXep,
            ThongSoChon = tsChon,
            Trang = trang,
            TongTrang = tongTrang,
            TongSo = tong,
            CayDanhMuc = DanhMucTree.Flatten(allDm).Select(x => new DanhMucMenuVM
            {
                Ten = x.Node.TenDanhMuc, Slug = x.Node.Slug, Level = x.Level, Active = x.Node.Slug == danhMuc
            }).ToList(),
            Breadcrumb = crumbs,
            ThuongHieus = hangFacet,
            Facets = facets
        });
    }

    // ================= GỢI Ý TÌM KIẾM (AJAX) =================
    [HttpGet("goi-y")]
    public async Task<IActionResult> GoiY(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Json(Array.Empty<object>());

        var query = _db.SanPhams.AsNoTracking().Where(s => !s.NgungBan);
        foreach (var t in TachTuKhoa(q))
            query = query.Where(s => s.TenSP.Contains(t) || s.MaSP.Contains(t));

        var rows = await query.OrderBy(s => s.MaSP).Take(8)
            .Select(s => new { s.MaSP, s.TenSP, s.Slug, s.HinhAnh, Gia = s.GiaKhuyenMai ?? s.Gia })
            .ToListAsync();

        return Json(rows.Select(r => new
        {
            maSP = r.MaSP,
            ten = r.TenSP,
            url = Url.Action("ChiTiet", "SanPham", new { slug = r.Slug }),
            hinhAnh = r.HinhAnh,
            gia = r.Gia.Vnd()
        }));
    }

    // ================= CHI TIẾT =================
    [HttpGet("{slug}")]
    public async Task<IActionResult> ChiTiet(string slug)
    {
        var sp = await _db.SanPhams.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug && !s.NgungBan);
        if (sp == null) return NotFound();

        var hang = await _db.ThuongHieus.AsNoTracking().FirstOrDefaultAsync(t => t.Id == sp.ThuongHieuId);
        var specs = await _db.Set<ThongSoKyThuat>().AsNoTracking()
            .Where(t => t.SanPhamId == sp.Id).OrderBy(t => t.Id)
            .Select(t => new ThongSoHienThiVM { Ten = t.TenThongSo, GiaTri = t.GiaTri, DonVi = t.DonVi })
            .ToListAsync();

        // Đường dẫn danh mục: Cha > Con
        var allDm = await _db.DanhMucs.AsNoTracking().ToListAsync();
        var byId = allDm.ToDictionary(d => d.Id);
        var crumbs = new List<(string Ten, string? Slug)>();
        var cur = byId.GetValueOrDefault(sp.DanhMucId);
        while (cur != null)
        {
            crumbs.Insert(0, (cur.TenDanhMuc, cur.Slug));
            cur = cur.DanhMucChaId.HasValue && byId.TryGetValue(cur.DanhMucChaId.Value, out var cha) ? cha : (DanhMuc?)null;
        }

        var lienQuan = await _db.SanPhams.AsNoTracking()
            .Where(s => !s.NgungBan && s.DanhMucId == sp.DanhMucId && s.Id != sp.Id)
            .OrderByDescending(s => s.Id).Take(4)
            .Select(SanPhamProjection.Card).ToListAsync();
        await SanPhamProjection.GanThuongHieuAsync(_db, lienQuan);

        return View(new ChiTietSanPhamVM
        {
            Id = sp.Id, MaSP = sp.MaSP, Ten = sp.TenSP, Slug = sp.Slug, MoTa = sp.MoTa,
            HinhAnh = sp.HinhAnh, DatasheetUrl = sp.DatasheetUrl,
            Gia = sp.Gia, GiaKhuyenMai = sp.GiaKhuyenMai,
            SoLuongTon = sp.SoLuongTon, MucTonToiThieu = sp.MucTonToiThieu,
            ThuongHieu = hang?.TenThuongHieu, QuocGia = hang?.QuocGia,
            Breadcrumb = crumbs, ThongSo = specs, LienQuan = lienQuan
        });
    }

    private static string[] TachTuKhoa(string? q) =>
        (q ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(5).ToArray();
}
