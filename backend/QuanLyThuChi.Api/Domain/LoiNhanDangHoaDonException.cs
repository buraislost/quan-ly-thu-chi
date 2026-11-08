namespace QuanLyThuChi.Domain;

/// <summary>Không đọc được hóa đơn nhưng ảnh đã được lưu; trả <see cref="AnhUrl"/> để giao diện vẫn đính kèm ảnh khi nhập tay.</summary>
public class LoiNhanDangHoaDonException(string message, string anhUrl) : DomainException(message)
{
    public string AnhUrl { get; } = anhUrl;
}
