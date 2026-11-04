namespace LinhKien.Models;

public class ChiTietGioHang
{
    public int Id { get; set; }
    public int GioHangId { get; set; }
    public GioHang? GioHang { get; set; }
    public int SanPhamId { get; set; }
    public SanPham? SanPham { get; set; }
    public int SoLuong { get; set; }
}