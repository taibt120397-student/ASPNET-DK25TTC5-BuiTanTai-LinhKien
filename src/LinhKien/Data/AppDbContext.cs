using LinhKien.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();
    public DbSet<ThuongHieu> ThuongHieus => Set<ThuongHieu>();
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<ThongSoKyThuat> ThongSos => Set<ThongSoKyThuat>();
    public DbSet<GioHang> GioHangs => Set<GioHang>();
    public DbSet<ChiTietGioHang> ChiTietGioHangs => Set<ChiTietGioHang>();
    public DbSet<DiaChiGiaoHang> DiaChiGiaoHangs => Set<DiaChiGiaoHang>();
    public DbSet<MaGiamGia> MaGiamGias => Set<MaGiamGia>();
    public DbSet<DonHang> DonHangs => Set<DonHang>();
    public DbSet<ChiTietDonHang> ChiTietDonHangs => Set<ChiTietDonHang>();
    public DbSet<DanhGia> DanhGias => Set<DanhGia>();
    public DbSet<NhatKyThaoTac> NhatKys => Set<NhatKyThaoTac>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Tên bảng đúng như đề cương
        b.Entity<DanhMuc>().ToTable("DanhMuc");
        b.Entity<ThuongHieu>().ToTable("ThuongHieu");
        b.Entity<SanPham>().ToTable("SanPham");
        b.Entity<ThongSoKyThuat>().ToTable("ThongSoKyThuat");
        b.Entity<GioHang>().ToTable("GioHang");
        b.Entity<ChiTietGioHang>().ToTable("ChiTietGioHang");
        b.Entity<DiaChiGiaoHang>().ToTable("DiaChiGiaoHang");
        b.Entity<MaGiamGia>().ToTable("MaGiamGia");
        b.Entity<DonHang>().ToTable("DonHang");
        b.Entity<ChiTietDonHang>().ToTable("ChiTietDonHang");
        b.Entity<DanhGia>().ToTable("DanhGia");
        b.Entity<NhatKyThaoTac>().ToTable("NhatKyThaoTac");

        // Cột tiền
        b.Entity<SanPham>().Property(x => x.Gia).HasPrecision(18, 0);
        b.Entity<SanPham>().Property(x => x.GiaKhuyenMai).HasPrecision(18, 0);
        b.Entity<ChiTietDonHang>().Property(x => x.DonGia).HasPrecision(18, 0);
        b.Entity<DonHang>().Property(x => x.TongTien).HasPrecision(18, 0);
        b.Entity<DonHang>().Property(x => x.PhiVanChuyen).HasPrecision(18, 0);
        b.Entity<DonHang>().Property(x => x.TienGiam).HasPrecision(18, 0);
        b.Entity<MaGiamGia>().Property(x => x.GiaTriGiam).HasPrecision(18, 0);
        b.Entity<MaGiamGia>().Property(x => x.DonToiThieu).HasPrecision(18, 0);

        // Index
        b.Entity<SanPham>().HasIndex(x => x.MaSP).IsUnique();
        b.Entity<SanPham>().HasIndex(x => new { x.DanhMucId, x.Gia });
        b.Entity<DonHang>().HasIndex(x => x.MaDon).IsUnique();
        b.Entity<DonHang>().HasIndex(x => new { x.NguoiDungId, x.NgayDat });
        b.Entity<MaGiamGia>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<DanhGia>().HasIndex(x => new { x.SanPhamId, x.NguoiDungId }).IsUnique();

        // Không xóa dây chuyền xuống đơn hàng
        b.Entity<ChiTietDonHang>().HasOne(x => x.SanPham).WithMany()
            .HasForeignKey(x => x.SanPhamId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DonHang>().HasOne(x => x.NguoiDung).WithMany()
            .HasForeignKey(x => x.NguoiDungId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DonHang>().HasOne(x => x.MaGiamGia).WithMany()
            .HasForeignKey(x => x.MaGiamGiaId).OnDelete(DeleteBehavior.Restrict);

        // Danh mục tự tham chiếu: không xóa dây chuyền danh mục con
        b.Entity<DanhMuc>().HasOne(x => x.DanhMucCha).WithMany()
            .HasForeignKey(x => x.DanhMucChaId).OnDelete(DeleteBehavior.Restrict);
    }
}