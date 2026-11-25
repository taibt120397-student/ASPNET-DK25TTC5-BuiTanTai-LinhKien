/* sp_DatHang: đặt hàng trong MỘT transaction.
   Đọc giỏ hàng -> khóa và kiểm tra tồn -> tính giá, mã giảm giá, phí vận chuyển ->
   tạo DonHang + ChiTietDonHang (chốt DonGia) -> trừ tồn -> tăng SoLanDaDung -> xóa giỏ.
   Thiếu tồn: ROLLBACK, trả về danh sách sản phẩm không đủ hàng (result set đầu tiên).
   MaLoi: 0 thành công | 1 giỏ trống | 2 thiếu tồn hoặc ngừng bán | 3 mã giảm giá không hợp lệ
          4 phương thức thanh toán sai | 5 lỗi khác */
CREATE OR ALTER PROCEDURE dbo.sp_DatHang
    @NguoiDungId         NVARCHAR(450),
    @TenNguoiNhan        NVARCHAR(100),
    @DienThoai           NVARCHAR(20),
    @DiaChiGiao          NVARCHAR(300),
    @PhuongThucThanhToan NVARCHAR(20),
    @MaGiamGia           NVARCHAR(30)  = NULL,
    @GhiChu              NVARCHAR(500) = NULL,
    @PhiCoDinh           DECIMAL(18,0) = 0,
    @NguongMienPhi       DECIMAL(18,0) = 500000,
    @DonHangId           INT           OUTPUT,
    @MaDon               NVARCHAR(30)  OUTPUT,
    @MaLoi               INT           OUTPUT,
    @ThongBao            NVARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @DonHangId = NULL;
    SET @MaDon     = NULL;
    SET @MaLoi     = 0;
    SET @ThongBao  = N'';

    IF @PhuongThucThanhToan NOT IN (N'COD', N'ChuyenKhoan')
    BEGIN
        SET @MaLoi = 4; SET @ThongBao = N'Phương thức thanh toán không hợp lệ.';
        RETURN;
    END

    DECLARE @Gio TABLE
    (
        SanPhamId INT,
        TenSP     NVARCHAR(200),
        SoLuong   INT,
        Ton       INT,
        DonGia    DECIMAL(18,0),
        NgungBan  BIT
    );

    DECLARE @GioHangId INT,
            @TienHang  DECIMAL(18,0),
            @TienGiam  DECIMAL(18,0) = 0,
            @Phi       DECIMAL(18,0),
            @Tong      DECIMAL(18,0),
            @Stt       INT,
            @MaGiamGiaId INT = NULL,
            @Ma        NVARCHAR(30) = LTRIM(RTRIM(ISNULL(@MaGiamGia, N''))),
            @LaPhanTram BIT, @GiaTri DECIMAL(18,0), @DonToiThieu DECIMAL(18,0),
            @NgayBd DATETIME2, @NgayKt DATETIME2, @ToiDa INT, @DaDung INT, @KichHoat BIT;

    BEGIN TRY
        BEGIN TRAN;

        SELECT @GioHangId = MIN(Id) FROM dbo.GioHang WHERE NguoiDungId = @NguoiDungId;

        -- Khóa các dòng sản phẩm trong giỏ (UPDLOCK) để người khác không mua trùng lúc đang kiểm tra
        INSERT INTO @Gio (SanPhamId, TenSP, SoLuong, Ton, DonGia, NgungBan)
        SELECT s.Id, s.TenSP, SUM(c.SoLuong), s.SoLuongTon, ISNULL(s.GiaKhuyenMai, s.Gia), s.NgungBan
        FROM dbo.ChiTietGioHang AS c
        JOIN dbo.SanPham AS s WITH (UPDLOCK, ROWLOCK) ON s.Id = c.SanPhamId
        WHERE c.GioHangId = @GioHangId
        GROUP BY s.Id, s.TenSP, s.SoLuongTon, s.GiaKhuyenMai, s.Gia, s.NgungBan;

        IF NOT EXISTS (SELECT 1 FROM @Gio)
        BEGIN
            ROLLBACK;
            SET @MaLoi = 1; SET @ThongBao = N'Giỏ hàng đang trống.';
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM @Gio WHERE NgungBan = 1 OR Ton < SoLuong)
        BEGIN
            SELECT SanPhamId, TenSP, SoLuong AS SoLuongYeuCau, Ton AS SoLuongCon, NgungBan
            FROM @Gio WHERE NgungBan = 1 OR Ton < SoLuong;
            ROLLBACK;
            SET @MaLoi = 2; SET @ThongBao = N'Một số sản phẩm không đủ hàng hoặc đã ngừng bán.';
            RETURN;
        END

        SELECT @TienHang = SUM(SoLuong * DonGia) FROM @Gio;

        -- Mã giảm giá (khóa dòng mã để hai đơn không cùng dùng lượt cuối)
        IF @Ma <> N''
        BEGIN
            SELECT @MaGiamGiaId = Id, @LaPhanTram = LaPhanTram, @GiaTri = GiaTriGiam, @DonToiThieu = DonToiThieu,
                   @NgayBd = NgayBatDau, @NgayKt = NgayKetThuc, @ToiDa = SoLanToiDa, @DaDung = SoLanDaDung, @KichHoat = KichHoat
            FROM dbo.MaGiamGia WITH (UPDLOCK, ROWLOCK)
            WHERE Ma = @Ma;

            IF @MaGiamGiaId IS NULL OR @KichHoat = 0 OR GETDATE() < @NgayBd OR GETDATE() > @NgayKt OR @DaDung >= @ToiDa
            BEGIN
                ROLLBACK;
                SET @MaLoi = 3; SET @ThongBao = N'Mã giảm giá không hợp lệ, đã hết hạn hoặc hết lượt sử dụng.';
                RETURN;
            END

            IF @TienHang < @DonToiThieu
            BEGIN
                ROLLBACK;
                SET @MaLoi = 3; SET @ThongBao = N'Đơn hàng chưa đạt giá trị tối thiểu để dùng mã này.';
                RETURN;
            END

            SET @TienGiam = CASE WHEN @LaPhanTram = 1 THEN ROUND(@TienHang * @GiaTri / 100, 0) ELSE @GiaTri END;
            IF @TienGiam > @TienHang SET @TienGiam = @TienHang;
        END

        -- Phí vận chuyển: miễn phí khi tiền hàng (trước giảm giá) đạt ngưỡng
        SET @Phi  = CASE WHEN @TienHang >= @NguongMienPhi THEN 0 ELSE @PhiCoDinh END;
        SET @Tong = @TienHang - @TienGiam + @Phi;

        -- Mã đơn: DH + yyyyMMdd + số thứ tự trong ngày (khóa dải để không trùng)
        SELECT @Stt = COUNT(*) + 1
        FROM dbo.DonHang WITH (UPDLOCK, HOLDLOCK)
        WHERE NgayDat >= CAST(CAST(GETDATE() AS DATE) AS DATETIME2)
          AND NgayDat <  DATEADD(DAY, 1, CAST(CAST(GETDATE() AS DATE) AS DATETIME2));

        SET @MaDon = N'DH' + CONVERT(NVARCHAR(8), GETDATE(), 112)
                   + CASE WHEN @Stt < 1000 THEN RIGHT(N'000' + CAST(@Stt AS NVARCHAR(10)), 3)
                          ELSE CAST(@Stt AS NVARCHAR(10)) END;

        INSERT INTO dbo.DonHang
            (MaDon, NguoiDungId, NgayDat, TrangThai, PhuongThucThanhToan, TrangThaiThanhToan,
             TenNguoiNhan, DienThoai, DiaChiGiao, PhiVanChuyen, MaGiamGiaId, TienGiam, TongTien, GhiChu)
        VALUES
            (@MaDon, @NguoiDungId, GETDATE(), N'ChoXacNhan', @PhuongThucThanhToan, N'ChuaThanhToan',
             @TenNguoiNhan, @DienThoai, @DiaChiGiao, @Phi, @MaGiamGiaId, @TienGiam, @Tong, @GhiChu);

        SET @DonHangId = SCOPE_IDENTITY();

        -- Chốt đơn giá tại thời điểm đặt
        INSERT INTO dbo.ChiTietDonHang (DonHangId, SanPhamId, SoLuong, DonGia)
        SELECT @DonHangId, SanPhamId, SoLuong, DonGia FROM @Gio;

        -- Trừ tồn kho
        UPDATE s SET s.SoLuongTon = s.SoLuongTon - g.SoLuong
        FROM dbo.SanPham AS s
        JOIN @Gio AS g ON g.SanPhamId = s.Id;

        IF @MaGiamGiaId IS NOT NULL
            UPDATE dbo.MaGiamGia SET SoLanDaDung = SoLanDaDung + 1 WHERE Id = @MaGiamGiaId;

        -- Xóa giỏ
        DELETE FROM dbo.ChiTietGioHang WHERE GioHangId = @GioHangId;
        UPDATE dbo.GioHang SET NgayCapNhat = GETDATE() WHERE Id = @GioHangId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SET @DonHangId = NULL;
        SET @MaDon     = NULL;
        SET @MaLoi     = 5;
        SET @ThongBao  = LEFT(ERROR_MESSAGE(), 300);
    END CATCH
END
