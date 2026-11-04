namespace LinhKien.Models;

public class ChiTietDonHang
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public DonHang? DonHang { get; set; }
    public int SanPhamId { get; set; }
    public SanPham? SanPham { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }     // giá chốt tại thời điểm đặt
    public decimal ThanhTien => SoLuong * DonGia;
}