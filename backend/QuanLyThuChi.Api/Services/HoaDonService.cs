using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Services;

public class HoaDonService(LuuTepService luuTep, IDichVuOCR ocr)
{
    /// <summary>Kiểm tra ảnh rồi lưu ngay; nhận dạng lỗi thì giữ ảnh và báo kèm URL để người dùng nhập tay.</summary>
    public async Task<BanNhapHoaDonDto> QuetAsync(IFormFile tep, CancellationToken ct)
    {
        var anh = await luuTep.DocAnhAsync(tep, ct);
        var url = await luuTep.GhiAsync(anh, ct);
        KetQuaOCR kq;
        try
        {
            kq = await ocr.NhanDangAsync(anh.DuLieu, anh.Mime, ct);
        }
        catch (DomainException ex) when (ex is not LoiNhanDangHoaDonException)
        {
            throw new LoiNhanDangHoaDonException(ex.Message, url);
        }
        return BanNhapHoaDonDto.Tu(url, kq);
    }
}
