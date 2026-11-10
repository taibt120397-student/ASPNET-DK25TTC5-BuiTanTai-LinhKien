namespace LinhKien.Services;

public interface IImageService
{
    /// <summary>Trả về thông báo lỗi, hoặc null nếu file hợp lệ.</summary>
    string? Validate(IFormFile file);
    /// <summary>Lưu ảnh sản phẩm, trả về đường dẫn tương đối (vd /uploads/san-pham/abc.jpg).</summary>
    Task<string> SaveProductImageAsync(IFormFile file);
    void Delete(string? relativePath);
}

public class ImageService : IImageService
{
    private const string Folder = "/uploads/san-pham/";
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IWebHostEnvironment _env;

    public ImageService(IWebHostEnvironment env) => _env = env;

    public string? Validate(IFormFile file)
    {
        if (file.Length == 0) return "File ảnh rỗng.";
        if (file.Length > MaxBytes) return "Ảnh tối đa 2 MB.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext)) return "Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .webp.";

        // Kiểm tra "magic bytes" để chặn file đổi đuôi (vd .exe đổi thành .jpg)
        using var s = file.OpenReadStream();
        Span<byte> h = stackalloc byte[12];
        var n = s.Read(h);
        var isJpg = n >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;
        var isPng = n >= 4 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47;
        var isWebp = n >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                     && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50;
        return isJpg || isPng || isWebp ? null : "Nội dung file không phải ảnh hợp lệ.";
    }

    public async Task<string> SaveProductImageAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var name = $"{Guid.NewGuid():N}{ext}"; // tên ngẫu nhiên, không dùng tên người dùng gửi lên
        var dir = Path.Combine(_env.WebRootPath, "uploads", "san-pham");
        Directory.CreateDirectory(dir);
        await using var fs = new FileStream(Path.Combine(dir, name), FileMode.Create);
        await file.CopyToAsync(fs);
        return Folder + name;
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        if (!relativePath.StartsWith(Folder) || relativePath.Contains("..")) return;
        var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(full)) File.Delete(full);
    }
}
