namespace QuanLyThuChi.Infrastructure;

public record MatHangOCR(string Ten, decimal SoLuong, decimal DonGia);

public record KetQuaOCR(string? TenCuaHang, DateOnly? Ngay, decimal? TongTien, List<MatHangOCR> MatHang);

/// <summary>Nhận dạng hóa đơn từ ảnh. Mọi lỗi nhận dạng đều ném <c>DomainException("Không nhận dạng được hóa đơn")</c>.</summary>
public interface IDichVuOCR
{
    Task<KetQuaOCR> NhanDangAsync(byte[] anh, string mimeType, CancellationToken ct);
}
