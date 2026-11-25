/* sp_HuyDonHang: hủy đơn và hoàn tồn trong MỘT transaction.
   Khách (@LaNhanVien = 0): chỉ hủy đơn của chính mình, khi đơn ở trạng thái Chờ xác nhận hoặc Đã xác nhận.
   Nhân viên (@LaNhanVien = 1): hủy được mọi đơn chưa Hoàn thành và chưa Hủy.
   Hoàn SoLuongTon từng dòng, hoàn 1 lượt dùng mã giảm giá (nếu có).
   MaLoi: 0 thành công | 1 không tìm thấy đơn | 2 không được phép hủy | 5 lỗi khác */
CREATE OR ALTER PROCEDURE dbo.sp_HuyDonHang
    @DonHangId   INT,
    @NguoiDungId NVARCHAR(450) = NULL,
    @LaNhanVien  BIT = 0,
    @MaLoi       INT           OUTPUT,
    @ThongBao    NVARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MaLoi    = 0;
    SET @ThongBao = N'';

    DECLARE @TrangThai NVARCHAR(30), @ChuDon NVARCHAR(450), @MaGiamGiaId INT;

    BEGIN TRY
        BEGIN TRAN;

        SELECT @TrangThai = TrangThai, @ChuDon = NguoiDungId, @MaGiamGiaId = MaGiamGiaId
        FROM dbo.DonHang WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @DonHangId;

        -- Khách không phải chủ đơn cũng nhận thông báo "không tìm thấy" để không lộ sự tồn tại của đơn
        IF @TrangThai IS NULL OR (@LaNhanVien = 0 AND (@NguoiDungId IS NULL OR @NguoiDungId <> @ChuDon))
        BEGIN
            ROLLBACK;
            SET @MaLoi = 1; SET @ThongBao = N'Không tìm thấy đơn hàng.';
            RETURN;
        END

        IF @TrangThai = N'DaHuy'
        BEGIN
            ROLLBACK;
            SET @MaLoi = 2; SET @ThongBao = N'Đơn hàng đã được hủy trước đó.';
            RETURN;
        END

        IF @TrangThai = N'HoanThanh'
        BEGIN
            ROLLBACK;
            SET @MaLoi = 2; SET @ThongBao = N'Đơn hàng đã hoàn thành, không thể hủy.';
            RETURN;
        END

        IF @LaNhanVien = 0 AND @TrangThai NOT IN (N'ChoXacNhan', N'DaXacNhan')
        BEGIN
            ROLLBACK;
            SET @MaLoi = 2; SET @ThongBao = N'Đơn hàng đang được giao, bạn không thể tự hủy. Vui lòng liên hệ cửa hàng.';
            RETURN;
        END

        -- Hoàn tồn từng sản phẩm
        UPDATE s SET s.SoLuongTon = s.SoLuongTon + c.SoLuong
        FROM dbo.SanPham AS s
        JOIN (SELECT SanPhamId, SUM(SoLuong) AS SoLuong
              FROM dbo.ChiTietDonHang WHERE DonHangId = @DonHangId GROUP BY SanPhamId) AS c
          ON c.SanPhamId = s.Id;

        UPDATE dbo.DonHang SET TrangThai = N'DaHuy' WHERE Id = @DonHangId;

        IF @MaGiamGiaId IS NOT NULL
            UPDATE dbo.MaGiamGia
            SET SoLanDaDung = CASE WHEN SoLanDaDung > 0 THEN SoLanDaDung - 1 ELSE 0 END
            WHERE Id = @MaGiamGiaId;

        COMMIT;
        SET @ThongBao = N'Đã hủy đơn hàng và hoàn lại tồn kho.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SET @MaLoi    = 5;
        SET @ThongBao = LEFT(ERROR_MESSAGE(), 300);
    END CATCH
END
