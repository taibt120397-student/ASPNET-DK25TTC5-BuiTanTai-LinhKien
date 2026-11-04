namespace LinhKien.Models;

public class ThongSoKyThuat
{
    public int Id { get; set; }
    public int SanPhamId { get; set; }
    public SanPham? SanPham { get; set; }
    public string TenThongSo { get; set; } = "";   // vd: Điện áp hoạt động
    public string GiaTri { get; set; } = "";       // vd: 5
    public string? DonVi { get; set; }             // vd: V
}