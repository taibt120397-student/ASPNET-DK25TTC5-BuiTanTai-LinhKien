using System.ComponentModel.DataAnnotations;

namespace LinhKien.ViewModels;

// ---------- Danh mục ----------
public class DanhMucFormVM
{
    public int Id { get; set; }

    [Display(Name = "Tên danh mục")]
    [Required(ErrorMessage = "Nhập tên danh mục"), StringLength(100)]
    public string TenDanhMuc { get; set; } = "";

    [Display(Name = "Slug (để trống sẽ tự tạo)")]
    [StringLength(120)]
    public string? Slug { get; set; }

    [Display(Name = "Danh mục cha")]
    public int? DanhMucChaId { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    [Range(0, 9999, ErrorMessage = "Thứ tự từ 0 đến 9999")]
    public int ThuTu { get; set; }
}

public class DanhMucRow
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public string? Slug { get; set; }
    public int Level { get; set; }
    public int ThuTu { get; set; }
    public int SoCon { get; set; }
    public int SoSanPham { get; set; }
}

// ---------- Thương hiệu ----------
public class ThuongHieuFormVM
{
    public int Id { get; set; }

    [Display(Name = "Tên thương hiệu")]
    [Required(ErrorMessage = "Nhập tên thương hiệu"), StringLength(100)]
    public string TenThuongHieu { get; set; } = "";

    [Display(Name = "Quốc gia")]
    [StringLength(60)]
    public string? QuocGia { get; set; }
}

public class ThuongHieuRow
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public string? QuocGia { get; set; }
    public int SoSanPham { get; set; }
}

// ---------- Sản phẩm ----------
public class ThongSoVM
{
    public string? TenThongSo { get; set; }
    public string? GiaTri { get; set; }
    public string? DonVi { get; set; }
}

public class SanPhamFormVM
{
    public int Id { get; set; }

    [Display(Name = "Mã sản phẩm")]
    public string? MaSP { get; set; }

    [Display(Name = "Tên sản phẩm")]
    [Required(ErrorMessage = "Nhập tên sản phẩm"), StringLength(200)]
    public string TenSP { get; set; } = "";

    [Display(Name = "Slug (để trống sẽ tự tạo)")]
    [StringLength(220)]
    public string? Slug { get; set; }

    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Display(Name = "Giá (₫)")]
    [Range(1, 1_000_000_000, ErrorMessage = "Giá phải lớn hơn 0")]
    public decimal Gia { get; set; }

    [Display(Name = "Giá khuyến mãi (₫)")]
    [Range(0, 1_000_000_000, ErrorMessage = "Giá khuyến mãi không hợp lệ")]
    public decimal? GiaKhuyenMai { get; set; }

    [Display(Name = "Số lượng tồn")]
    [Range(0, 1_000_000, ErrorMessage = "Tồn kho từ 0 đến 1.000.000")]
    public int SoLuongTon { get; set; }

    [Display(Name = "Mức tồn tối thiểu")]
    [Range(0, 1_000_000, ErrorMessage = "Mức tồn tối thiểu không hợp lệ")]
    public int MucTonToiThieu { get; set; }

    [Display(Name = "Danh mục")]
    [Required(ErrorMessage = "Chọn danh mục")]
    public int? DanhMucId { get; set; }

    [Display(Name = "Thương hiệu")]
    [Required(ErrorMessage = "Chọn thương hiệu")]
    public int? ThuongHieuId { get; set; }

    [Display(Name = "Liên kết datasheet")]
    [StringLength(500)]
    public string? DatasheetUrl { get; set; }

    [Display(Name = "Ngừng bán (ẩn khỏi cửa hàng)")]
    public bool NgungBan { get; set; }

    /// <summary>Đường dẫn ảnh hiện tại (chỉ để hiển thị).</summary>
    public string? HinhAnh { get; set; }

    [Display(Name = "Ảnh sản phẩm")]
    public IFormFile? Anh { get; set; }

    public byte[]? RowVersion { get; set; }

    public List<ThongSoVM> ThongSo { get; set; } = new();
}

public class SanPhamRow
{
    public int Id { get; set; }
    public string MaSP { get; set; } = "";
    public string TenSP { get; set; } = "";
    public string DanhMuc { get; set; } = "";
    public string ThuongHieu { get; set; } = "";
    public decimal Gia { get; set; }
    public decimal? GiaKhuyenMai { get; set; }
    public int SoLuongTon { get; set; }
    public int MucTonToiThieu { get; set; }
    public bool NgungBan { get; set; }
    public string? HinhAnh { get; set; }
}

public class SanPhamIndexVM
{
    public List<SanPhamRow> Rows { get; set; } = new();
    public string? Q { get; set; }
    public int? DanhMucId { get; set; }
    public int? ThuongHieuId { get; set; }
    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; } = 1;
    public int TongSo { get; set; }
}
