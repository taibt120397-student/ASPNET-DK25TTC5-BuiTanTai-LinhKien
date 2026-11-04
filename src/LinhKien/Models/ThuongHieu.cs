namespace LinhKien.Models;

public class ThuongHieu
{
    public int Id { get; set; }
    public string TenThuongHieu { get; set; } = "";
    public string? QuocGia { get; set; }
    public ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}