namespace QuanLyThuChi.Infrastructure;

public class GeminiOptions
{
    public const string TenMuc = "Gemini";

    /// <summary>Khóa API; chỉ đặt trong appsettings.Development.json hoặc biến môi trường, không đưa vào git.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gemini-3.8-flash";

    /// <summary>Bật để dùng dữ liệu hóa đơn mẫu, không gọi Gemini (demo offline).</summary>
    public bool DungBanGia { get; set; }
}
