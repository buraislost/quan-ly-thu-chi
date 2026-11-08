using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Services;

public class HoaDonService(LuuTepService luuTep, IDichVuOCR ocr)
{
    /// <summary>Kiểm tra ảnh, nhận dạng, và chỉ lưu tệp khi nhận dạng thành công.</summary>
    public async Task<BanNhapHoaDonDto> QuetAsync(IFormFile tep, CancellationToken ct)
    {
        var anh = await luuTep.DocAnhAsync(tep, ct);
        var kq = await ocr.NhanDangAsync(anh.DuLieu, anh.Mime, ct);
        var url = await luuTep.GhiAsync(anh, ct);
        return BanNhapHoaDonDto.Tu(url, kq);
    }
}
