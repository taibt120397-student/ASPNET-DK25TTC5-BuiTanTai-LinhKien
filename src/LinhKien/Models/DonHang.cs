using System.ComponentModel.DataAnnotations;

namespace LinhKien.Models;

public class DonHang
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string MaDon { get; set; } = "";         // DH20261104001

    public string NguoiDungId { get; set; } = "";
    public ApplicationUser? NguoiDung { get; set; }

    public DateTime NgayDat { get; set; } = DateTime.Now;
    public string TrangThai { get; set; } = TrangThaiDon.ChoXacNhan;

    public string PhuongThucThanhToan { get; set; } = "COD";      // COD | ChuyenKhoan
    public string TrangThaiThanhToan { get; set; } = "ChuaThanhToan";

    [Required, StringLength(100)]
    public string TenNguoiNhan { get; set; } = "";
    [Required, StringLength(20)]
    public string DienThoai { get; set; } = "";
    [Required, StringLength(300)]
    public string DiaChiGiao { get; set; } = "";

    public decimal PhiVanChuyen { get; set; }
    public int? MaGiamGiaId { get; set; }
    public MaGiamGia? MaGiamGia { get; set; }
    public decimal TienGiam { get; set; }
    public decimal TongTien { get; set; }
    public string? GhiChu { get; set; }

    public ICollection<ChiTietDonHang> ChiTiets { get; set; } = new List<ChiTietDonHang>();
}