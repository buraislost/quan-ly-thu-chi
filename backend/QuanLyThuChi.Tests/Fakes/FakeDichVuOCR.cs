using QuanLyThuChi.Domain;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Tests.Fakes;

/// <summary>OCR giả cho test: trả kết quả đặt sẵn hoặc ném lỗi như khi Gemini không đọc được.</summary>
public class FakeDichVuOCR : IDichVuOCR
{
    public static KetQuaOCR KetQuaMacDinh() => new(
        "Bách Hóa Xanh", new DateOnly(2026, 11, 7), 75_000m,
        [new MatHangOCR("Sữa tươi", 2, 15_000m), new MatHangOCR("Gạo ST25", 1, 45_000m)]);

    public KetQuaOCR KetQua { get; set; } = KetQuaMacDinh();

    public bool NemLoi { get; set; }

    public int SoLanGoi { get; private set; }

    public string? MimeCuoi { get; private set; }

    public Task<KetQuaOCR> NhanDangAsync(byte[] anh, string mimeType, CancellationToken ct)
    {
        SoLanGoi++;
        MimeCuoi = mimeType;
        if (NemLoi) throw new DomainException("Không nhận dạng được hóa đơn");
        return Task.FromResult(KetQua);
    }
}
