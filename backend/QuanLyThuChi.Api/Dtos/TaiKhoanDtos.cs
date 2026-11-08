using System.ComponentModel.DataAnnotations;

namespace QuanLyThuChi.Dtos;

public record DangKyRequest(
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    string HoTen,
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(200, ErrorMessage = "Email tối đa 200 ký tự")]
    string Email,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [MaxLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự")]
    string MatKhau);

public record DangNhapRequest(
    [Required(ErrorMessage = "Vui lòng nhập email")] string Email,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")] string MatKhau);

public record DangNhapResponse(string Token, string VaiTro, string HoTen);

public record CapNhatHoSoRequest(
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    string HoTen);

public record DoiMatKhauRequest(
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")] string MatKhauCu,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [MaxLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự")]
    string MatKhauMoi);

public record HoSoResponse(int MaTaiKhoan, string HoTen, string Email, string VaiTro, string? DonViTienTe);
