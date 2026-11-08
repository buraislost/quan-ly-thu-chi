using System.ComponentModel.DataAnnotations;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Dtos;

public record NguoiDungAdminDto(
    int MaTaiKhoan, string HoTen, string Email, TrangThaiTaiKhoan TrangThai, DateTime NgayTao, int SoGiaoDich);

public record KhoaTaiKhoanRequest(
    [Required(ErrorMessage = "Vui lòng nhập lý do khóa")]
    [MaxLength(500, ErrorMessage = "Lý do tối đa 500 ký tự")]
    string LyDo);

public record ThongKeDto(int SoNguoiDung, int SoDangHoatDong, int SoBiKhoa, int SoGiaoDich, int SoGiaoDichThangNay);

public record KetQuaTongKetDto(int SoNguoiDaXuLy);
