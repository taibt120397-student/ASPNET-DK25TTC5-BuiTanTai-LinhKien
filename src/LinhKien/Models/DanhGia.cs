using System.ComponentModel.DataAnnotations;

namespace LinhKien.Models;

public class DanhGia
{
    public int Id { get; set; }
    public int SanPhamId { get; set; }
    public SanPham? SanPham { get; set; }
    public string NguoiDungId { get; set; } = "";
    public ApplicationUser? NguoiDung { get; set; }

    [Range(1, 5)]
    public int SoSao { get; set; }
    public string? NoiDung { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
}