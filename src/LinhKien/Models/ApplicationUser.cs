using Microsoft.AspNetCore.Identity;

namespace LinhKien.Models;

public class ApplicationUser : IdentityUser
{
    public string? HoTen { get; set; }
}