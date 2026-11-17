using System.Globalization;

namespace LinhKien.ViewModels;

// ---------- Thẻ sản phẩm dùng chung (trang chủ, danh sách, sản phẩm liên quan) ----------
public class SanPhamCardVM
{
    public int Id { get; set; }
    public string MaSP { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? Slug { get; set; }
    public string? HinhAnh { get; set; }
    public int ThuongHieuId { get; set; }
    public string? ThuongHieuTen { get; set; }
    public decimal Gia { get; set; }
    public decimal? GiaKhuyenMai { get; set; }
    public int SoLuongTon { get; set; }
    public int MucTonToiThieu { get; set; }

    /// <summary>Giá khách phải trả.</summary>
    public decimal GiaBan => GiaKhuyenMai ?? Gia;

    public int PhanTramGiam =>
        GiaKhuyenMai.HasValue && Gia > 0 ? (int)Math.Round((1 - GiaKhuyenMai.Value / Gia) * 100) : 0;
}

// ---------- Trang chủ ----------
public class DanhMucNhomVM
{
    public string Ten { get; set; } = "";
    public string? Slug { get; set; }
    public List<(string Ten, string? Slug)> Con { get; set; } = new();
}

public class TrangChuVM
{
    public List<DanhMucNhomVM> DanhMuc { get; set; } = new();
    public List<SanPhamCardVM> KhuyenMai { get; set; } = new();
    public List<SanPhamCardVM> Moi { get; set; } = new();
}

// ---------- Danh sách, tìm kiếm, lọc ----------
public class DanhMucMenuVM
{
    public string Ten { get; set; } = "";
    public string? Slug { get; set; }
    public int Level { get; set; }
    public bool Active { get; set; }
}

public class ThuongHieuFacetVM
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public int Dem { get; set; }
}

public class FacetGiaTriVM
{
    public string GiaTri { get; set; } = "";
    public string? DonVi { get; set; }
    public int Dem { get; set; }
    public bool Chon { get; set; }
}

public class FacetThongSoVM
{
    public string Ten { get; set; } = "";
    public List<FacetGiaTriVM> GiaTri { get; set; } = new();
}

public class SanPhamListVM
{
    public List<SanPhamCardVM> Items { get; set; } = new();

    // Bộ lọc hiện tại
    public string? Q { get; set; }
    public string? DanhMuc { get; set; }
    public string? TenDanhMuc { get; set; }
    public int? ThuongHieu { get; set; }
    public decimal? GiaTu { get; set; }
    public decimal? GiaDen { get; set; }
    public bool ConHang { get; set; }
    public string SapXep { get; set; } = "moi";
    public List<string> ThongSoChon { get; set; } = new();

    // Phân trang
    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; } = 1;
    public int TongSo { get; set; }

    // Dữ liệu cho thanh bên
    public List<DanhMucMenuVM> CayDanhMuc { get; set; } = new();
    public List<(string Ten, string? Slug)> Breadcrumb { get; set; } = new();
    public List<ThuongHieuFacetVM> ThuongHieus { get; set; } = new();
    public List<FacetThongSoVM> Facets { get; set; } = new();

    public bool CoLoc => ThuongHieu.HasValue || GiaTu.HasValue || GiaDen.HasValue || ConHang || ThongSoChon.Count > 0;

    private static string Build(IEnumerable<(string k, string? v)> pairs)
    {
        var parts = pairs.Where(p => !string.IsNullOrEmpty(p.v))
            .Select(p => $"{Uri.EscapeDataString(p.k)}={Uri.EscapeDataString(p.v!)}");
        var qs = string.Join("&", parts);
        return "/san-pham" + (qs == "" ? "" : "?" + qs);
    }

    /// <summary>Đường dẫn sang trang p, giữ nguyên toàn bộ bộ lọc.</summary>
    public string LinkTrang(int p)
    {
        var pairs = new List<(string, string?)>
        {
            ("q", Q), ("danhMuc", DanhMuc),
            ("thuongHieu", ThuongHieu?.ToString()),
            ("giaTu", GiaTu?.ToString(CultureInfo.InvariantCulture)),
            ("giaDen", GiaDen?.ToString(CultureInfo.InvariantCulture)),
            ("conHang", ConHang ? "true" : null),
            ("sapXep", SapXep == "moi" ? null : SapXep)
        };
        pairs.AddRange(ThongSoChon.Select(t => ("ts", (string?)t)));
        pairs.Add(("trang", p > 1 ? p.ToString() : null));
        return Build(pairs);
    }

    /// <summary>Đổi danh mục: giữ từ khóa và cách sắp xếp, bỏ các bộ lọc khác.</summary>
    public string LinkDanhMuc(string? slug) =>
        Build(new List<(string, string?)> { ("q", Q), ("danhMuc", slug), ("sapXep", SapXep == "moi" ? null : SapXep) });
}

// ---------- Chi tiết sản phẩm ----------
public class ThongSoHienThiVM
{
    public string Ten { get; set; } = "";
    public string GiaTri { get; set; } = "";
    public string? DonVi { get; set; }
}

public class ChiTietSanPhamVM
{
    public int Id { get; set; }
    public string MaSP { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? Slug { get; set; }
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public string? DatasheetUrl { get; set; }
    public decimal Gia { get; set; }
    public decimal? GiaKhuyenMai { get; set; }
    public int SoLuongTon { get; set; }
    public int MucTonToiThieu { get; set; }
    public string? ThuongHieu { get; set; }
    public string? QuocGia { get; set; }
    public List<(string Ten, string? Slug)> Breadcrumb { get; set; } = new();
    public List<ThongSoHienThiVM> ThongSo { get; set; } = new();
    public List<SanPhamCardVM> LienQuan { get; set; } = new();

    public decimal GiaBan => GiaKhuyenMai ?? Gia;
    public int PhanTramGiam =>
        GiaKhuyenMai.HasValue && Gia > 0 ? (int)Math.Round((1 - GiaKhuyenMai.Value / Gia) * 100) : 0;

    public string TinhTrang =>
        SoLuongTon <= 0 ? "Hết hàng"
        : SoLuongTon <= MucTonToiThieu ? $"Sắp hết hàng (còn {SoLuongTon})"
        : "Còn hàng";
}

// ---------- Giỏ hàng ----------
public class GioHangDong
{
    public int ChiTietId { get; set; }
    public int SanPhamId { get; set; }
    public string MaSP { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? Slug { get; set; }
    public string? HinhAnh { get; set; }
    public decimal Gia { get; set; }
    public decimal? GiaKhuyenMai { get; set; }
    public int SoLuong { get; set; }
    public int Ton { get; set; }
    public bool NgungBan { get; set; }

    public decimal DonGia => GiaKhuyenMai ?? Gia;
    public decimal ThanhTien => DonGia * SoLuong;

    public string? CanhBao =>
        NgungBan ? "Sản phẩm đã ngừng bán"
        : Ton <= 0 ? "Hết hàng"
        : SoLuong > Ton ? $"Chỉ còn {Ton} sản phẩm"
        : null;
}

public class GioHangVM
{
    public List<GioHangDong> Dong { get; set; } = new();
    public decimal TongTien => Dong.Sum(d => d.ThanhTien);
    public bool CoCanhBao => Dong.Any(d => d.CanhBao != null);
}
