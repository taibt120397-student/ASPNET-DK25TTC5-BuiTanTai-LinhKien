using System.ComponentModel.DataAnnotations;

namespace LinhKien.Models;

public class SanPham
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string MaSP { get; set; } = "";          // LK0001

    [Required, StringLength(200)]
    public string TenSP { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? MoTa { get; set; }

    public decimal Gia { get; set; }
    public decimal? GiaKhuyenMai { get; set; }

    public int SoLuongTon { get; set; }
    public int MucTonToiThieu { get; set; } = 5;

    public int DanhMucId { get; set; }
    public DanhMuc? DanhMuc { get; set; }
    public int ThuongHieuId { get; set; }
    public ThuongHieu? ThuongHieu { get; set; }

    public string? HinhAnh { get; set; }
    public string? DatasheetUrl { get; set; }
    public bool NgungBan { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }          // chống bán quá tồn khi đặt cùng lúc

    public ICollection<ThongSoKyThuat> ThongSos { get; set; } = new List<ThongSoKyThuat>();
    public ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();

    // Giá thực tế khách phải trả
    public decimal GiaBan => GiaKhuyenMai ?? Gia;
}