using System.ComponentModel.DataAnnotations;

namespace QuanLyThuChi.Dtos;

/// <param name="TienDo">Phần trăm đã chi trên hạn mức, làm tròn 1 chữ số thập phân.</param>
public record NganSachDto(
    int MaNganSach, int MaDanhMuc, string TenDanhMuc, decimal HanMuc, decimal SoDaChi, double TienDo, int NguongCanhBao);

public record TaoNganSachRequest(
    int MaDanhMuc,
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Hạn mức phải lớn hơn 0")]
    decimal HanMuc,
    [Range(1, 12, ErrorMessage = "Tháng không hợp lệ")] int Thang,
    [Range(2000, 2100, ErrorMessage = "Năm không hợp lệ")] int Nam,
    [Range(1, 100, ErrorMessage = "Ngưỡng cảnh báo phải từ 1 đến 100")] int? NguongCanhBao = null);

/// <summary>Danh mục và tháng của ngân sách không đổi được; bỏ trống ngưỡng thì giữ nguyên.</summary>
public record SuaNganSachRequest(
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Hạn mức phải lớn hơn 0")]
    decimal HanMuc,
    [Range(1, 100, ErrorMessage = "Ngưỡng cảnh báo phải từ 1 đến 100")] int? NguongCanhBao = null);
