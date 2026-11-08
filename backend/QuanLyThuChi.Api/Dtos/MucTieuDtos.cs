using System.ComponentModel.DataAnnotations;

namespace QuanLyThuChi.Dtos;

/// <param name="TienDo">Phần trăm đã nạp trên mục tiêu, làm tròn 1 chữ số thập phân.</param>
public record MucTieuDto(
    int MaMucTieu, string TenMucTieu, decimal SoTienMucTieu, decimal SoTienHienTai, DateOnly HanChot,
    double TienDo, decimal SoTienCanMoiThang, bool DaHoanThanh);

public record TaoMucTieuRequest(
    [Required(ErrorMessage = "Tên mục tiêu không được để trống")]
    [StringLength(200, ErrorMessage = "Tên mục tiêu tối đa 200 ký tự")] string TenMucTieu,
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Số tiền mục tiêu phải lớn hơn 0")]
    decimal SoTienMucTieu,
    DateOnly HanChot);

public record SuaMucTieuRequest(
    [Required(ErrorMessage = "Tên mục tiêu không được để trống")]
    [StringLength(200, ErrorMessage = "Tên mục tiêu tối đa 200 ký tự")] string TenMucTieu,
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Số tiền mục tiêu phải lớn hơn 0")]
    decimal SoTienMucTieu,
    DateOnly HanChot);

public record NapTienRequest(
    int MaVi,
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Số tiền phải lớn hơn 0")] decimal SoTien);
