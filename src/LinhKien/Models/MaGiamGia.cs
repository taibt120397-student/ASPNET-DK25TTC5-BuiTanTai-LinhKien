using System.ComponentModel.DataAnnotations;

namespace LinhKien.Models;

public class MaGiamGia
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Ma { get; set; } = "";

    public bool LaPhanTram { get; set; }       // true: giảm theo %, false: giảm số tiền
    public decimal GiaTriGiam { get; set; }    // 10 (%) hoặc 50000 (đ)
    public decimal DonToiThieu { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public int SoLanToiDa { get; set; }
    public int SoLanDaDung { get; set; }
    public bool KichHoat { get; set; } = true;
}