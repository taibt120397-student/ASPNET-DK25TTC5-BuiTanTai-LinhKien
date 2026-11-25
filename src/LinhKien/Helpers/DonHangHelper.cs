using LinhKien.Models;

namespace LinhKien.Helpers;

/// <summary>Chuyển mã trạng thái thành chữ tiếng Việt và màu hiển thị.</summary>
public static class DonHangHelper
{
    public static readonly string[] CacBuoc =
        { TrangThaiDon.ChoXacNhan, TrangThaiDon.DaXacNhan, TrangThaiDon.DangGiao, TrangThaiDon.HoanThanh };

    public static string TenTrangThai(string t) => t switch
    {
        TrangThaiDon.ChoXacNhan => "Chờ xác nhận",
        TrangThaiDon.DaXacNhan => "Đã xác nhận",
        TrangThaiDon.DangGiao => "Đang giao",
        TrangThaiDon.HoanThanh => "Hoàn thành",
        TrangThaiDon.DaHuy => "Đã hủy",
        _ => t
    };

    public static string Badge(string t) => t switch
    {
        TrangThaiDon.ChoXacNhan => "text-bg-warning",
        TrangThaiDon.DaXacNhan => "text-bg-info",
        TrangThaiDon.DangGiao => "text-bg-primary",
        TrangThaiDon.HoanThanh => "text-bg-success",
        TrangThaiDon.DaHuy => "text-bg-secondary",
        _ => "text-bg-light"
    };

    public static string TenPhuongThuc(string p) =>
        p == PhuongThucThanhToan.COD ? "Thanh toán khi nhận hàng (COD)" : "Chuyển khoản ngân hàng";

    public static string TenThanhToan(string t) =>
        t == TrangThaiThanhToan.DaThanhToan ? "Đã thanh toán" : "Chưa thanh toán";

    /// <summary>Chỉ số bước hiện tại trong 4 bước chính (-1 nếu đã hủy).</summary>
    public static int Buoc(string t) => Array.IndexOf(CacBuoc, t);

    public static bool KhachHuyDuoc(string t) => t is TrangThaiDon.ChoXacNhan or TrangThaiDon.DaXacNhan;

    /// <summary>Trạng thái tiếp theo trong vòng đời đơn; null nếu không còn bước nào.</summary>
    public static string? TiepTheo(string t) => t switch
    {
        TrangThaiDon.ChoXacNhan => TrangThaiDon.DaXacNhan,
        TrangThaiDon.DaXacNhan => TrangThaiDon.DangGiao,
        TrangThaiDon.DangGiao => TrangThaiDon.HoanThanh,
        _ => null
    };

    public static string NhanNutTiep(string t) => t switch
    {
        TrangThaiDon.ChoXacNhan => "Xác nhận đơn",
        TrangThaiDon.DaXacNhan => "Giao hàng",
        TrangThaiDon.DangGiao => "Hoàn thành",
        _ => ""
    };
}
