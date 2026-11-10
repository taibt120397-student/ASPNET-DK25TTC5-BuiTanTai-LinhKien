using LinhKien.Helpers;
using LinhKien.Models;
using Microsoft.EntityFrameworkCore;

namespace LinhKien.Data;

/// <summary>
/// Nạp dữ liệu mẫu: danh mục nhiều cấp, thương hiệu, ~46 sản phẩm kèm thông số kỹ thuật.
/// Chỉ chạy khi bảng DanhMuc còn trống nên gọi nhiều lần vẫn an toàn.
/// Ảnh sản phẩm để trống – upload qua trang quản trị.
/// </summary>
public static class CatalogSeeder
{
    // Specs: "Tên=Giá trị|Đơn vị; Tên=Giá trị" (các mục cách nhau bằng dấu ;)
    private record Sp(string Ten, string DanhMuc, string ThuongHieu, decimal Gia, decimal? Km,
                      int Ton, int Min, string Specs, string? Datasheet = null);

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.DanhMucs.AnyAsync()) return;

        await using var tx = await db.Database.BeginTransactionAsync();

        // ---------- 1. Danh mục (2 cấp) ----------
        var cay = new (string Cha, string[] Con)[]
        {
            ("Vi điều khiển & Board", new[] { "Arduino", "ESP32 / ESP8266", "Raspberry Pi" }),
            ("Cảm biến",              new[] { "Cảm biến nhiệt độ - độ ẩm", "Cảm biến khoảng cách", "Cảm biến ánh sáng & chuyển động" }),
            ("Module",                new[] { "Module Wifi - Bluetooth", "Module Relay", "Module hiển thị & LED" }),
            ("Linh kiện thụ động",    new[] { "Điện trở", "Tụ điện" }),
            ("IC & Bán dẫn",          new[] { "IC & vi mạch", "Transistor - MOSFET" }),
            ("Dụng cụ & Phụ kiện",    new[] { "Breadboard & dây nối", "Dụng cụ hàn" }),
        };

        var cha = cay.Select((x, i) => new DanhMuc
        {
            TenDanhMuc = x.Cha, Slug = SlugHelper.ToSlug(x.Cha), DanhMucChaId = null, ThuTu = i + 1
        }).ToList();
        db.DanhMucs.AddRange(cha);
        await db.SaveChangesAsync();

        for (var i = 0; i < cay.Length; i++)
        {
            var con = cay[i].Con.Select((ten, j) => new DanhMuc
            {
                TenDanhMuc = ten, Slug = SlugHelper.ToSlug(ten), DanhMucChaId = cha[i].Id, ThuTu = j + 1
            });
            db.DanhMucs.AddRange(con);
        }
        await db.SaveChangesAsync();
        var dmId = await db.DanhMucs.ToDictionaryAsync(d => d.TenDanhMuc, d => d.Id);

        // ---------- 2. Thương hiệu ----------
        var hieu = new (string Ten, string QuocGia)[]
        {
            ("Arduino", "Ý"), ("Espressif", "Trung Quốc"), ("Raspberry Pi", "Anh"),
            ("Texas Instruments", "Mỹ"), ("STMicroelectronics", "Thụy Sĩ"), ("Infineon", "Đức"),
            ("Bosch Sensortec", "Đức"), ("Yageo", "Đài Loan"), ("Vishay", "Mỹ"),
            ("Adafruit", "Mỹ"), ("Aosong", "Trung Quốc"), ("Songle", "Trung Quốc"),
            ("Hakko", "Nhật Bản"), ("Generic", "Trung Quốc"),
        };
        db.ThuongHieus.AddRange(hieu.Select(h => new ThuongHieu { TenThuongHieu = h.Ten, QuocGia = h.QuocGia }));
        await db.SaveChangesAsync();
        var thId = await db.ThuongHieus.ToDictionaryAsync(t => t.TenThuongHieu, t => t.Id);

        // ---------- 3. Sản phẩm + thông số ----------
        var data = DuLieuSanPham();
        var dsSp = new List<(SanPham Sp, Sp Nguon)>();
        var slugDaDung = new HashSet<string>();
        for (var n = 0; n < data.Count; n++)
        {
            var s = data[n];
            var slug = SlugHelper.ToSlug(s.Ten);
            if (!slugDaDung.Add(slug)) slug = $"{slug}-{n + 1}";

            dsSp.Add((new SanPham
            {
                MaSP = $"LK{n + 1:D4}",
                TenSP = s.Ten,
                Slug = slug,
                MoTa = $"{s.Ten} thuộc nhóm {s.DanhMuc}, thương hiệu {s.ThuongHieu}. " +
                       "Phù hợp cho học tập, làm dự án DIY và chế tạo nguyên mẫu.",
                Gia = s.Gia,
                GiaKhuyenMai = s.Km,
                SoLuongTon = s.Ton,
                MucTonToiThieu = s.Min,
                DanhMucId = dmId[s.DanhMuc],
                ThuongHieuId = thId[s.ThuongHieu],
                DatasheetUrl = s.Datasheet,
                NgungBan = false
            }, s));
        }
        db.SanPhams.AddRange(dsSp.Select(x => x.Sp));
        await db.SaveChangesAsync(); // sinh Id cho sản phẩm

        foreach (var (sp, nguon) in dsSp)
        {
            foreach (var part in nguon.Specs.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                var gv = kv[1].Split('|', 2);
                db.ThongSos.Add(new ThongSoKyThuat
                {
                    SanPhamId = sp.Id,
                    TenThongSo = kv[0].Trim(),
                    GiaTri = gv[0].Trim(),
                    DonVi = gv.Length > 1 && gv[1].Trim() != "" ? gv[1].Trim() : null
                });
            }
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static List<Sp> DuLieuSanPham() => new()
    {
        // ===== Arduino =====
        new("Arduino Uno R3 (chính hãng)", "Arduino", "Arduino", 285000, 259000, 40, 10,
            "Vi điều khiển=ATmega328P; Điện áp hoạt động=5|V; Chân I/O số=14|chân; Bộ nhớ Flash=32|KB; Xung nhịp=16|MHz",
            "https://docs.arduino.cc/resources/datasheets/A000066-datasheet.pdf"),
        new("Arduino Nano V3 (chip CH340)", "Arduino", "Generic", 95000, null, 60, 15,
            "Vi điều khiển=ATmega328P; Điện áp hoạt động=5|V; Bộ nhớ Flash=32|KB; Cổng kết nối=Mini-USB"),
        new("Arduino Mega 2560 R3", "Arduino", "Arduino", 520000, 479000, 20, 5,
            "Vi điều khiển=ATmega2560; Điện áp hoạt động=5|V; Chân I/O số=54|chân; Bộ nhớ Flash=256|KB; Xung nhịp=16|MHz"),
        new("Arduino Pro Micro (ATmega32U4)", "Arduino", "Generic", 110000, null, 25, 8,
            "Vi điều khiển=ATmega32U4; Điện áp hoạt động=5|V; Bộ nhớ Flash=32|KB; Hỗ trợ USB gốc=Có"),

        // ===== ESP32 / ESP8266 =====
        new("ESP32 DevKit V1 (30 chân)", "ESP32 / ESP8266", "Espressif", 135000, 119000, 50, 10,
            "Chip=ESP32-WROOM-32; Điện áp logic=3.3|V; Xung nhịp=240|MHz; Bộ nhớ Flash=4|MB; Kết nối=Wifi 802.11 b/g/n + Bluetooth 4.2 BLE",
            "https://www.espressif.com/sites/default/files/documentation/esp32_datasheet_en.pdf"),
        new("ESP32-C3 SuperMini", "ESP32 / ESP8266", "Espressif", 89000, null, 35, 10,
            "Chip=ESP32-C3 (RISC-V); Điện áp logic=3.3|V; Xung nhịp=160|MHz; Bộ nhớ Flash=4|MB; Kết nối=Wifi + Bluetooth 5 LE"),
        new("NodeMCU ESP8266 CP2102 (ESP-12E)", "ESP32 / ESP8266", "Espressif", 85000, 75000, 45, 10,
            "Chip=ESP8266EX; Điện áp logic=3.3|V; Xung nhịp=80|MHz; Bộ nhớ Flash=4|MB; Kết nối=Wifi 802.11 b/g/n"),
        new("ESP32-CAM kèm camera OV2640", "ESP32 / ESP8266", "Espressif", 175000, null, 18, 5,
            "Chip=ESP32-S; Camera=OV2640 2MP; Khe thẻ nhớ=MicroSD; Điện áp cấp=5|V; Kết nối=Wifi + Bluetooth"),

        // ===== Raspberry Pi =====
        new("Raspberry Pi Pico", "Raspberry Pi", "Raspberry Pi", 98000, null, 30, 8,
            "Chip=RP2040 (Cortex-M0+ 2 nhân); Xung nhịp=133|MHz; RAM=264|KB; Bộ nhớ Flash=2|MB; Điện áp logic=3.3|V"),
        new("Raspberry Pi Pico W", "Raspberry Pi", "Raspberry Pi", 145000, 135000, 22, 6,
            "Chip=RP2040 (Cortex-M0+ 2 nhân); Xung nhịp=133|MHz; RAM=264|KB; Bộ nhớ Flash=2|MB; Kết nối=Wifi 2.4 GHz"),
        new("Raspberry Pi 4 Model B 4GB", "Raspberry Pi", "Raspberry Pi", 1790000, null, 8, 3,
            "Chip=Broadcom BCM2711 (Cortex-A72 4 nhân); Xung nhịp=1.5|GHz; RAM=4|GB; Nguồn cấp=5|V; Cổng=2 micro-HDMI, 4 USB, Gigabit Ethernet"),

        // ===== Cảm biến nhiệt độ - độ ẩm =====
        new("Cảm biến nhiệt độ độ ẩm DHT11", "Cảm biến nhiệt độ - độ ẩm", "Aosong", 22000, null, 120, 30,
            "Dải nhiệt độ=0 – 50|°C; Sai số nhiệt độ=±2|°C; Dải độ ẩm=20 – 90|%RH; Điện áp hoạt động=3 – 5.5|V"),
        new("Cảm biến nhiệt độ độ ẩm DHT22 (AM2302)", "Cảm biến nhiệt độ - độ ẩm", "Aosong", 85000, 79000, 70, 15,
            "Dải nhiệt độ=-40 – 80|°C; Sai số nhiệt độ=±0.5|°C; Dải độ ẩm=0 – 100|%RH; Điện áp hoạt động=3.3 – 6|V"),
        new("Cảm biến BME280 (nhiệt độ, độ ẩm, áp suất)", "Cảm biến nhiệt độ - độ ẩm", "Bosch Sensortec", 98000, null, 28, 8,
            "Đại lượng đo=Nhiệt độ, độ ẩm, áp suất khí quyển; Giao tiếp=I2C / SPI; Điện áp hoạt động=1.8 – 3.6|V"),
        new("Cảm biến nhiệt độ DS18B20 chống nước", "Cảm biến nhiệt độ - độ ẩm", "Generic", 38000, null, 55, 15,
            "Dải nhiệt độ=-55 – 125|°C; Sai số=±0.5|°C; Giao tiếp=1-Wire; Điện áp hoạt động=3 – 5.5|V; Chiều dài dây=1|m"),

        // ===== Cảm biến khoảng cách =====
        new("Cảm biến siêu âm HC-SR04", "Cảm biến khoảng cách", "Generic", 28000, null, 80, 20,
            "Tầm đo=2 – 400|cm; Điện áp hoạt động=5|V; Góc đo=15|°; Giao tiếp=Trig / Echo"),
        new("Cảm biến khoảng cách ToF VL53L0X", "Cảm biến khoảng cách", "STMicroelectronics", 105000, 95000, 20, 6,
            "Tầm đo=3 – 200|cm; Giao tiếp=I2C; Điện áp hoạt động=2.6 – 3.5|V; Công nghệ=Laser Time-of-Flight"),
        new("Cảm biến hồng ngoại tránh vật cản", "Cảm biến khoảng cách", "Generic", 15000, null, 100, 25,
            "Tầm phát hiện=2 – 30|cm; Điện áp hoạt động=3.3 – 5|V; Ngõ ra=Digital"),

        // ===== Cảm biến ánh sáng & chuyển động =====
        new("Cảm biến chuyển động PIR HC-SR501", "Cảm biến ánh sáng & chuyển động", "Generic", 25000, null, 60, 15,
            "Khoảng cách phát hiện=3 – 7|m; Góc quét=120|°; Điện áp hoạt động=4.5 – 20|V"),
        new("Quang trở LDR GL5528 (gói 10 con)", "Cảm biến ánh sáng & chuyển động", "Generic", 15000, null, 300, 60,
            "Điện trở khi sáng=10 – 20|kΩ; Điện trở khi tối=1|MΩ; Điện áp tối đa=150|V; Công suất=100|mW"),
        new("Cảm biến ánh sáng BH1750", "Cảm biến ánh sáng & chuyển động", "Generic", 35000, null, 6, 8,
            "Dải đo=1 – 65535|lux; Giao tiếp=I2C; Điện áp hoạt động=3 – 5|V"),

        // ===== Module Wifi - Bluetooth =====
        new("Module Wifi ESP-01S ESP8266", "Module Wifi - Bluetooth", "Espressif", 38000, null, 40, 10,
            "Chip=ESP8266; Điện áp hoạt động=3.3|V; Giao tiếp=UART; Bộ nhớ Flash=1|MB"),
        new("Module Bluetooth HC-05", "Module Wifi - Bluetooth", "Generic", 95000, 85000, 24, 6,
            "Chuẩn Bluetooth=2.0 + EDR; Chế độ=Master / Slave; Điện áp hoạt động=3.3 – 5|V; Giao tiếp=UART"),
        new("Module Bluetooth BLE HM-10", "Module Wifi - Bluetooth", "Generic", 105000, null, 14, 5,
            "Chuẩn Bluetooth=4.0 BLE; Chip=CC2541; Điện áp hoạt động=3.3|V; Giao tiếp=UART"),

        // ===== Module Relay =====
        new("Module Relay 1 kênh 5V 10A (opto cách ly)", "Module Relay", "Songle", 18000, null, 90, 20,
            "Điện áp cuộn hút=5|V; Tải tối đa=10|A; Điện áp tải=250 VAC / 30 VDC; Kích hoạt=Mức thấp"),
        new("Module Relay 4 kênh 5V 10A", "Module Relay", "Songle", 55000, 49000, 35, 10,
            "Số kênh=4; Điện áp cuộn hút=5|V; Tải tối đa=10|A; Kích hoạt=Mức thấp"),

        // ===== Module hiển thị & LED =====
        new("Màn hình LCD 1602 kèm module I2C", "Module hiển thị & LED", "Generic", 45000, null, 50, 12,
            "Kích thước hiển thị=16 x 2|ký tự; Giao tiếp=I2C (địa chỉ 0x27); Điện áp hoạt động=5|V; Đèn nền=Xanh dương"),
        new("Màn hình OLED 0.96 inch I2C SSD1306", "Module hiển thị & LED", "Generic", 65000, 59000, 45, 10,
            "Độ phân giải=128 x 64|px; Giao tiếp=I2C; Điện áp hoạt động=3.3 – 5|V; Màu hiển thị=Trắng"),
        new("Vòng LED RGB NeoPixel 16 bóng", "Module hiển thị & LED", "Adafruit", 310000, null, 12, 4,
            "Số LED=16; Chip điều khiển=WS2812B; Điện áp hoạt động=5|V; Giao tiếp=1 dây dữ liệu"),

        // ===== Điện trở =====
        new("Bộ điện trở 1/4W 1% (600 con, 30 giá trị)", "Điện trở", "Yageo", 95000, null, 40, 10,
            "Công suất=0.25|W; Sai số=1|%; Số lượng=600|con; Loại=Điện trở màng kim loại"),
        new("Điện trở 10kΩ 1/4W (gói 100 con)", "Điện trở", "Yageo", 8000, null, 200, 50,
            "Giá trị=10|kΩ; Công suất=0.25|W; Sai số=5|%"),
        new("Điện trở sứ công suất 5W 10Ω", "Điện trở", "Vishay", 6000, null, 100, 20,
            "Giá trị=10|Ω; Công suất=5|W; Sai số=5|%; Loại=Điện trở sứ"),

        // ===== Tụ điện =====
        new("Tụ hóa 100µF 25V (gói 20 con)", "Tụ điện", "Yageo", 12000, null, 150, 40,
            "Điện dung=100|µF; Điện áp định mức=25|V; Loại=Tụ hóa nhôm; Sai số=20|%"),
        new("Tụ gốm 104 100nF 50V (gói 100 con)", "Tụ điện", "Yageo", 15000, null, 250, 60,
            "Điện dung=100|nF; Điện áp định mức=50|V; Loại=Tụ gốm"),
        new("Tụ hóa 1000µF 16V (gói 10 con)", "Tụ điện", "Yageo", 14000, null, 90, 25,
            "Điện dung=1000|µF; Điện áp định mức=16|V; Loại=Tụ hóa nhôm"),

        // ===== IC & vi mạch =====
        new("IC định thời NE555 DIP-8", "IC & vi mạch", "Texas Instruments", 6000, null, 150, 40,
            "Điện áp cấp=4.5 – 16|V; Dòng ngõ ra=200|mA; Kiểu chân=DIP-8",
            "https://www.ti.com/lit/ds/symlink/ne555.pdf"),
        new("IC khuếch đại thuật toán LM358", "IC & vi mạch", "Texas Instruments", 5000, null, 120, 30,
            "Số kênh=2; Điện áp cấp=3 – 32|V; Kiểu chân=DIP-8",
            "https://www.ti.com/lit/ds/symlink/lm358.pdf"),
        new("IC điều khiển động cơ L293D DIP-16", "IC & vi mạch", "Texas Instruments", 28000, 25000, 40, 10,
            "Số kênh=4 (2 cầu H); Điện áp động cơ=4.5 – 36|V; Dòng mỗi kênh=600|mA; Kiểu chân=DIP-16",
            "https://www.ti.com/lit/ds/symlink/l293d.pdf"),
        new("IC dịch bit 74HC595 DIP-16", "IC & vi mạch", "Texas Instruments", 8000, null, 100, 25,
            "Số bit=8; Điện áp cấp=2 – 6|V; Kiểu chân=DIP-16; Loại=Shift register nối tiếp sang song song"),
        new("IC ổn áp L7805CV 5V TO-220", "IC & vi mạch", "STMicroelectronics", 6000, null, 130, 30,
            "Điện áp ra=5|V; Dòng ra tối đa=1.5|A; Điện áp vào=7 – 35|V; Kiểu chân=TO-220",
            "https://www.st.com/resource/en/datasheet/l78.pdf"),

        // ===== Transistor - MOSFET =====
        new("MOSFET kênh N IRF540N TO-220", "Transistor - MOSFET", "Infineon", 18000, null, 70, 15,
            "Điện áp Vds=100|V; Dòng Id=33|A; Điện trở Rds(on)=44|mΩ; Kiểu chân=TO-220"),
        new("Transistor NPN 2N2222A (gói 10 con)", "Transistor - MOSFET", "STMicroelectronics", 12000, null, 90, 20,
            "Loại=NPN; Điện áp Vce=40|V; Dòng Ic=800|mA; Kiểu chân=TO-92"),

        // ===== Breadboard & dây nối =====
        new("Breadboard 830 lỗ (MB-102)", "Breadboard & dây nối", "Generic", 55000, null, 60, 15,
            "Số lỗ=830; Kích thước=165 x 55 x 10|mm; Chất liệu=Nhựa ABS"),
        new("Bộ dây Dupont 3 loại x 40 sợi (120 sợi)", "Breadboard & dây nối", "Generic", 28000, null, 80, 20,
            "Số sợi=120; Chiều dài=20|cm; Đầu nối=Đực-đực, đực-cái, cái-cái"),

        // ===== Dụng cụ hàn =====
        new("Mỏ hàn thiếc điều chỉnh nhiệt 60W", "Dụng cụ hàn", "Generic", 135000, 119000, 25, 6,
            "Công suất=60|W; Dải nhiệt độ=200 – 450|°C; Điện áp=220|V"),
        new("Trạm hàn Hakko FX-888DX", "Dụng cụ hàn", "Hakko", 3150000, null, 2, 2,
            "Công suất=70|W; Dải nhiệt độ=120 – 480|°C; Điện áp=220|V; Màn hình=LED hiển thị nhiệt độ"),
        new("Thiếc hàn 0.8mm 60/40 lõi nhựa thông 100g", "Dụng cụ hàn", "Generic", 65000, null, 40, 10,
            "Đường kính=0.8|mm; Hợp kim=Sn60/Pb40; Khối lượng=100|g"),
    };
}
