using System.Security.Claims;
using LinhKien.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinhKien.Controllers;

[Route("gio-hang")]
public class GioHangController : Controller
{
    private readonly IGioHangService _gio;

    public GioHangController(IGioHangService gio) => _gio = gio;

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // ---------- Trang giỏ hàng (bắt buộc đăng nhập) ----------
    [Authorize]
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _gio.LayAsync(UserId!));

    [Authorize]
    [HttpPost("cap-nhat"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhat(int id, int soLuong)
    {
        var kq = await _gio.CapNhatAsync(UserId!, id, soLuong);
        TempData[kq.Ok ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost("xoa"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var kq = await _gio.XoaAsync(UserId!, id);
        TempData[kq.Ok ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    // ---------- Các hàm AJAX (trả JSON) ----------

    /// <summary>Thêm vào giỏ từ trang danh sách/chi tiết. Chưa đăng nhập thì trả 401 để JS chuyển sang trang đăng nhập.</summary>
    [HttpPost("them"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(int sanPhamId, int soLuong = 1)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { ok = false, thongBao = "Vui lòng đăng nhập để thêm vào giỏ hàng.", dem = 0 });

        var kq = await _gio.ThemAsync(UserId!, sanPhamId, soLuong);
        return Json(new { ok = kq.Ok, thongBao = kq.ThongBao, dem = kq.Dem });
    }

    /// <summary>Số dòng trong giỏ, hiển thị trên biểu tượng giỏ hàng ở thanh menu.</summary>
    [HttpGet("so-luong")]
    public async Task<IActionResult> SoLuong()
    {
        if (User.Identity?.IsAuthenticated != true) return Json(new { count = 0 });
        return Json(new { count = await _gio.DemAsync(UserId!) });
    }
}
