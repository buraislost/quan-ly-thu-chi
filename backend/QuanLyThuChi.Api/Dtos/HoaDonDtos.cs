using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Dtos;

public record MatHangNhapDto(string Ten, decimal SoLuong, decimal DonGia);

/// <summary>Bản nháp hóa đơn sau khi quét; người dùng chỉnh sửa rồi mới gửi kèm giao dịch.</summary>
public record BanNhapHoaDonDto(string AnhUrl, string? TenCuaHang, DateOnly? Ngay, decimal TongTien, List<MatHangNhapDto> MatHang)
{
    /// <summary>Không đọc được tổng tiền thì lấy tổng các dòng.</summary>
    public static BanNhapHoaDonDto Tu(string anhUrl, KetQuaOCR kq)
    {
        var dong = kq.MatHang.Select(m => new MatHangNhapDto(m.Ten, m.SoLuong, m.DonGia)).ToList();
        return new BanNhapHoaDonDto(anhUrl, kq.TenCuaHang, kq.Ngay,
            kq.TongTien ?? dong.Sum(m => m.SoLuong * m.DonGia), dong);
    }
}
