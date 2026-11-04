namespace LinhKien.Models;

public class NhatKyThaoTac
{
    public int Id { get; set; }
    public DateTime ThoiGian { get; set; } = DateTime.Now;
    public string? NguoiDungId { get; set; }    // NULL khi do trigger ghi
    public string HanhDong { get; set; } = "";
    public string BangTacDong { get; set; } = "";
    public string? KhoaBanGhi { get; set; }
    public string? GiaTriCu { get; set; }
    public string? GiaTriMoi { get; set; }
}