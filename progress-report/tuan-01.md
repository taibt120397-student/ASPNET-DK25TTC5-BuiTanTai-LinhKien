# Báo cáo tiến độ – Tuần 1

| Thông tin | Nội dung |
| :---- | :---- |
| Đề tài | Xây dựng website bán linh kiện điện tử trên nền tảng ASP.NET Core (LinhKien) |
| Sinh viên | Bùi Tấn Tài |
| MSSV | 170125376 |
| Lớp | DK25TTC5 |
| Giảng viên hướng dẫn | TS. Nguyễn Nhứt Lam |
| Thời gian báo cáo | Tuần 1 (02/11/2026 – 08/11/2026) |
| Repository | <https://github.com/taibt120397-student/ASPNET-DK25TTC5-BuiTanTai-LinhKien> |

## 1. Mục tiêu của tuần

Khởi tạo dự án, hoàn thiện mô hình dữ liệu và cấu hình đăng nhập, phân quyền, cụ thể:

- Chốt đề cương, use case và ERD.
- Tạo các entity, `DbContext` và migration đầu tiên để sinh cơ sở dữ liệu SQL Server.
- Cấu hình ASP.NET Core Identity và nạp sẵn vai trò, tài khoản quản trị.

## 2. Công việc đã thực hiện

| STT | Công việc | Kết quả | Trạng thái |
| :---- | :---- | :---- | :---- |
| 1 | Chuẩn bị môi trường | Cài đặt Visual Studio và SQL Server (hoàn tất ngày 04/11), tạo repository GitHub theo cấu trúc quy định | Hoàn thành |
| 2 | Chốt đề cương, use case, ERD | Xác nhận lại danh sách bảng và cột để hạn chế phải đổi migration ở các tuần sau. ERD lưu tại `docs/erd.png` | Hoàn thành |
| 3 | Tạo các entity | `ApplicationUser` (kế thừa `IdentityUser`, thêm `HoTen`), `DanhMuc`, `ThuongHieu`, `SanPham`, `ThongSoKyThuat`, `GioHang`, `ChiTietGioHang`, `DiaChiGiaoHang`, `MaGiamGia`, `DonHang`, `ChiTietDonHang`, `DanhGia`, `NhatKyThaoTac`; lớp hằng số `TrangThaiDon` cho trạng thái đơn hàng | Hoàn thành |
| 4 | Tạo `AppDbContext` | Khai báo các `DbSet`, đặt tên bảng theo đề cương, cấu hình độ chính xác cho các cột tiền, chỉ mục và ràng buộc khóa ngoại | Hoàn thành |
| 5 | Cấu hình `Program.cs` | Đăng ký `DbContext`, Identity (có vai trò), MVC và Razor Pages (giao diện đăng nhập, đăng ký của Identity) | Hoàn thành |
| 6 | Migration `Init` | Tạo migration và cập nhật cơ sở dữ liệu bằng `dotnet ef database update` | Hoàn thành |
| 7 | Seed dữ liệu khởi tạo (`SeedData`) | Tự tạo 3 vai trò `Admin`, `NhanVien`, `KhachHang` và tài khoản quản trị `admin@shop.local` khi ứng dụng khởi động | Hoàn thành |

Mã nguồn đã được đẩy lên GitHub với commit "Them model, DbContext, migration Init va SeedData".

## 3. Các quyết định kỹ thuật chính

| Nội dung | Cách thực hiện | Lý do |
| :---- | :---- | :---- |
| Tên bảng | Dùng `ToTable("SanPham")`, `ToTable("DonHang")`... | Khớp đề cương, tránh EF tự đặt tên số nhiều (`SanPhams`) |
| Cột tiền | `HasPrecision(18, 0)` cho `Gia`, `GiaKhuyenMai`, `DonGia`, `TongTien`... | Khai báo rõ độ chính xác, tránh cảnh báo và cắt số lẻ |
| Chống bán quá tồn kho | Cột `RowVersion` (`[Timestamp]`) trong `SanPham` | Khi nhiều người đặt cùng lúc, EF phát hiện xung đột cập nhật |
| Không xóa dây chuyền | `DeleteBehavior.Restrict` từ `ChiTietDonHang` tới `SanPham` | Xóa sản phẩm không làm mất lịch sử đơn hàng |
| Ràng buộc và chỉ mục | Chỉ mục duy nhất cho `MaSP`, `MaDon`, `Ma` (mã giảm giá); chỉ mục ghép cho `(DanhMucId, Gia)` và `(NguoiDungId, NgayDat)` | Đảm bảo không trùng mã, tăng tốc lọc và tra cứu đơn hàng |
| Giá thực tế | Thuộc tính tính toán `GiaBan = GiaKhuyenMai ?? Gia` | Một chỗ duy nhất quyết định giá khách phải trả |
| Trạng thái đơn hàng | Hằng số trong lớp `TrangThaiDon` | Tránh gõ chuỗi tay, giảm lỗi chính tả |

## 4. Kết quả đạt được và tự kiểm tra

- [x] Chạy được website
- [x] Đăng nhập được bằng tài khoản Admin
- [x] Mở SQL Server Management Studio thấy đủ các bảng, tên bảng đúng như đề cương

**Hình ảnh minh chứng** :

- Danh sách bảng trong SSMS: <p align="center">
  <img src="docs/tuan01-ssms-tables.png" alt="Danh sách bảng DB" width="720">
  <br>
  <em>Hình 1. Danh sách bảng DB</em>
</p>
- Đăng nhập thành công bằng tài khoản Admin: <p align="center">
  <img src="docs/tuan01-login-admin.png" alt="Dang nhap thanh cong admin" width="720">
  <br>
  <em>Hình 1. Đăng nhập thành công</em>
</p>

## 5. Khó khăn và cách giải quyết

- Không phát sinh khó khăn đáng kể

## 6. Kế hoạch tuần 2 (09/11 – 15/11)

- Xây dựng chức năng quản lý danh mục và sản phẩm.
- Upload ảnh sản phẩm.
- Phân quyền theo vai trò; gán vai trò `KhachHang` khi khách đăng ký.
- Bổ sung dữ liệu mẫu (danh mục, thương hiệu, khoảng 30–50 sản phẩm kèm thông số kỹ thuật) vào `SeedData`.

## 7. Nhận xét của giảng viên


