namespace LinhKien.Models;

public class DanhMuc
{
    public int Id { get; set; }
    public string TenDanhMuc { get; set; } = "";
    public string Slug { get; set; } = "";
    public int? DanhMucChaId { get; set; }
    public DanhMuc? DanhMucCha { get; set; }
    public int ThuTu { get; set; }
    public ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}