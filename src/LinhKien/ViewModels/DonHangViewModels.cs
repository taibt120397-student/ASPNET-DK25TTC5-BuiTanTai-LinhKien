using System.ComponentModel.DataAnnotations;

namespace LinhKien.ViewModels;

// ---------- Sổ địa chỉ ----------
public class DiaChiItemVM
{
    public int Id { get; set; }
    public string HoTenNguoiNhan { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public bool MacDinh { get; set; }
}

public class DiaChiFormVM
{
    public int Id { get; set; }

    [Display(Name = "Họ tên người nhận")]
    [Required(ErrorMessage = "Nhập họ tên người nhận"), StringLength(100)]
    public string HoTenNguoiNhan { get; set; } = "";

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ 0912345678)")]
    public string DienThoai { get; set; } = "";

    [Display(Name = "Địa chỉ giao hàng")]
    [Required(ErrorMessage = "Nhập địa chỉ giao hàng")]
    [StringLength(300, MinimumLength = 10, ErrorMessage = "Địa chỉ cần ghi rõ số nhà, đường, phường/xã, quận/huyện, tỉnh/thành")]
    public string DiaChi { get; set; } = "";

    [Display(Name = "Đặt làm địa chỉ mặc định")]
    public bool MacDinh { get; set; }
}

// ---------- Đặt hàng ----------
public class DatHangFormVM
{
    /// <summary>Id địa chỉ đã lưu; 0 = nhập địa chỉ mới.</summary>
    public int DiaChiId { get; set; }

    [Display(Name = "Họ tên người nhận")]
    public string? HoTenNguoiNhan { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? DienThoai { get; set; }

    [Display(Name = "Địa chỉ giao hàng")]
    public string? DiaChi { get; set; }

    [Display(Name = "Lưu địa chỉ này vào sổ địa chỉ")]
    public bool LuuDiaChi { get; set; } = true;

    public string PhuongThucThanhToan { get; set; } = "COD";

    [Display(Name = "Mã giảm giá")]
    [StringLength(30)]
    public string? MaGiamGia { get; set; }

    [Display(Name = "Ghi chú")]
    [StringLength(500)]
    public string? GhiChu { get; set; }
}

public class TamTinhVM
{
    public decimal TienHang { get; set; }
    public decimal TienGiam { get; set; }
    public decimal Phi { get; set; }
    public decimal Tong { get; set; }
    public bool MaHopLe { get; set; }
    public string? ThongBaoMa { get; set; }
}

public class DatHangPageVM
{
    public DatHangFormVM Form { get; set; } = new();
    public GioHangVM Gio { get; set; } = new();
    public List<DiaChiItemVM> DiaChis { get; set; } = new();
    public TamTinhVM TamTinh { get; set; } = new();
    public decimal PhiCoDinh { get; set; }
    public decimal NguongMienPhi { get; set; }
    public string NganHang { get; set; } = "";
    public string SoTaiKhoan { get; set; } = "";
    public string ChuTaiKhoan { get; set; } = "";
}

// ---------- Đơn hàng (khách và nhân viên) ----------
public class DonHangRowVM
{
    public int Id { get; set; }
    public string MaDon { get; set; } = "";
    public DateTime NgayDat { get; set; }
    public string TrangThai { get; set; } = "";
    public string PhuongThucThanhToan { get; set; } = "";
    public string TrangThaiThanhToan { get; set; } = "";
    public string TenNguoiNhan { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public decimal TongTien { get; set; }
    public int SoMon { get; set; }
}

public class DonHangDongVM
{
    public int SanPhamId { get; set; }
    public string MaSP { get; set; } = "";
    public string TenSP { get; set; } = "";
    public string? Slug { get; set; }
    public string? HinhAnh { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
}

public class DonHangChiTietVM
{
    public int Id { get; set; }
    public string MaDon { get; set; } = "";
    public DateTime NgayDat { get; set; }
    public string TrangThai { get; set; } = "";
    public string PhuongThucThanhToan { get; set; } = "";
    public string TrangThaiThanhToan { get; set; } = "";
    public string TenNguoiNhan { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string DiaChiGiao { get; set; } = "";
    public string? GhiChu { get; set; }
    public decimal PhiVanChuyen { get; set; }
    public decimal TienGiam { get; set; }
    public decimal TongTien { get; set; }
    public string? MaGiamGia { get; set; }
    public string? EmailKhach { get; set; }
    public List<DonHangDongVM> Dong { get; set; } = new();
    public decimal TienHang => Dong.Sum(d => d.ThanhTien);
}

public class DonHangListVM
{
    public List<DonHangRowVM> Rows { get; set; } = new();
    public string? TrangThai { get; set; }
    public string? TuKhoa { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; } = 1;
    public int TongSo { get; set; }
    public Dictionary<string, int> DemTheoTrangThai { get; set; } = new();

    public string Link(int? trang = null, string? trangThai = "__giu")
    {
        var tt = trangThai == "__giu" ? TrangThai : trangThai;
        var pairs = new List<(string, string?)>
        {
            ("trangThai", tt), ("tuKhoa", TuKhoa),
            ("tuNgay", TuNgay?.ToString("yyyy-MM-dd")), ("denNgay", DenNgay?.ToString("yyyy-MM-dd")),
            ("trang", trang is > 1 ? trang.ToString() : null)
        };
        var qs = string.Join("&", pairs.Where(p => !string.IsNullOrEmpty(p.Item2))
            .Select(p => $"{p.Item1}={Uri.EscapeDataString(p.Item2!)}"));
        return qs == "" ? "?" : "?" + qs;
    }
}

// ---------- Hồ sơ ----------
public class HoSoVM
{
    [Display(Name = "Họ tên")]
    [StringLength(100)]
    public string? HoTen { get; set; }

    [Display(Name = "Số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ 0912345678)")]
    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }
}

// ---------- Mã giảm giá (quản trị) ----------
public class MaGiamGiaFormVM
{
    public int Id { get; set; }

    [Display(Name = "Mã giảm giá")]
    [Required(ErrorMessage = "Nhập mã giảm giá"), StringLength(30)]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch ngang hoặc gạch dưới")]
    public string Ma { get; set; } = "";

    [Display(Name = "Hình thức giảm")]
    public bool LaPhanTram { get; set; } = true;

    [Display(Name = "Giá trị giảm")]
    [Range(1, 1_000_000_000, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
    public decimal GiaTriGiam { get; set; }

    [Display(Name = "Đơn tối thiểu (₫)")]
    [Range(0, 1_000_000_000, ErrorMessage = "Đơn tối thiểu không hợp lệ")]
    public decimal DonToiThieu { get; set; }

    [Display(Name = "Ngày bắt đầu")]
    [Required(ErrorMessage = "Chọn ngày bắt đầu")]
    public DateTime? NgayBatDau { get; set; }

    [Display(Name = "Ngày kết thúc")]
    [Required(ErrorMessage = "Chọn ngày kết thúc")]
    public DateTime? NgayKetThuc { get; set; }

    [Display(Name = "Số lần dùng tối đa")]
    [Range(1, 1_000_000, ErrorMessage = "Số lần dùng tối đa phải từ 1")]
    public int SoLanToiDa { get; set; } = 100;

    [Display(Name = "Đang kích hoạt")]
    public bool KichHoat { get; set; } = true;
}

public class MaGiamGiaRowVM
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public bool LaPhanTram { get; set; }
    public decimal GiaTriGiam { get; set; }
    public decimal DonToiThieu { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public int SoLanToiDa { get; set; }
    public int SoLanDaDung { get; set; }
    public bool KichHoat { get; set; }

    public string TinhTrang
    {
        get
        {
            var now = DateTime.Now;
            if (!KichHoat) return "Đã khóa";
            if (now < NgayBatDau) return "Chưa bắt đầu";
            if (now > NgayKetThuc) return "Hết hạn";
            if (SoLanDaDung >= SoLanToiDa) return "Hết lượt";
            return "Đang hiệu lực";
        }
    }
}
