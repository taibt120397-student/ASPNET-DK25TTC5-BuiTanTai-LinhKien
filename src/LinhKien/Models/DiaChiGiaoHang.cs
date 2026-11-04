using System.ComponentModel.DataAnnotations;

namespace LinhKien.Models;

public class DiaChiGiaoHang
{
    public int Id { get; set; }
    public string NguoiDungId { get; set; } = "";
    public ApplicationUser? NguoiDung { get; set; }

    [Required, StringLength(100)]
    public string HoTenNguoiNhan { get; set; } = "";

    [Required, StringLength(20)]
    public string DienThoai { get; set; } = "";

    [Required, StringLength(300)]
    public string DiaChi { get; set; } = "";

    public bool MacDinh { get; set; }
}