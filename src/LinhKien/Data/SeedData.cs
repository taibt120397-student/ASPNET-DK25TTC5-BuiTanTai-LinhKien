using LinhKien.Models;
using Microsoft.AspNetCore.Identity;

namespace LinhKien.Data;

public static class SeedData
{
    public static async Task InitAsync(IServiceProvider sp)
    {
        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var r in new[] { "Admin", "NhanVien", "KhachHang" })
            if (!await roleMgr.RoleExistsAsync(r))
                await roleMgr.CreateAsync(new IdentityRole(r));

        const string email = "admin@shop.local";
        if (await userMgr.FindByEmailAsync(email) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                HoTen = "Quản trị viên",
                EmailConfirmed = true
            };
            await userMgr.CreateAsync(admin, "Admin@123");
            await userMgr.AddToRoleAsync(admin, "Admin");
        }
        // TODO: thêm danh mục, thương hiệu, sản phẩm mẫu (làm ở tuần 2)
    }
}