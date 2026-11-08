using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Infrastructure;

/// <summary>Trả một hóa đơn mẫu cố định, dùng khi <c>Gemini:DungBanGia</c> bật (demo không có mạng/khóa API).</summary>
public class DichVuOCRMau(IDongHo dongHo) : IDichVuOCR
{
    public Task<KetQuaOCR> NhanDangAsync(byte[] anh, string mimeType, CancellationToken ct) =>
        Task.FromResult(new KetQuaOCR(
            "Bách Hóa Xanh",
            dongHo.HomNay,
            87_000m,
            [
                new MatHangOCR("Sữa tươi không đường", 2, 18_000m),
                new MatHangOCR("Trứng gà (vỉ 10)", 1, 32_000m),
                new MatHangOCR("Rau muống", 1, 19_000m),
            ]));
}
