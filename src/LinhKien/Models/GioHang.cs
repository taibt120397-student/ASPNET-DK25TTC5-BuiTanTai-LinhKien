namespace LinhKien.Models;

public class GioHang
{
    public int Id { get; set; }
    public string NguoiDungId { get; set; } = "";
    public ApplicationUser? NguoiDung { get; set; }
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;
    public ICollection<ChiTietGioHang> ChiTiets { get; set; } = new List<ChiTietGioHang>();
}