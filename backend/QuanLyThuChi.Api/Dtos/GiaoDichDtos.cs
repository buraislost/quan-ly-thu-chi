using System.ComponentModel.DataAnnotations;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Dtos;

public record ChiTietRequest(
    [Required(ErrorMessage = "Tên mặt hàng không được để trống")]
    [StringLength(200, ErrorMessage = "Tên mặt hàng tối đa 200 ký tự")] string TenMatHang,
    decimal SoLuong,
    decimal DonGia);

public record HoaDonRequest(
    [StringLength(200, ErrorMessage = "Tên cửa hàng tối đa 200 ký tự")] string? TenCuaHang,
    DateTime? NgayHoaDon,
    [Required(ErrorMessage = "Vui lòng đính kèm ảnh hóa đơn")]
    [StringLength(500, ErrorMessage = "Đường dẫn ảnh hóa đơn quá dài")] string AnhHoaDon,
    [Required(ErrorMessage = "Chi tiết hóa đơn không được để trống")] List<ChiTietRequest> ChiTiet);

/// <param name="NgayGiaoDich">Bỏ trống thì lấy thời điểm hiện tại theo giờ Việt Nam.</param>
public record TaoGiaoDichRequest(
    int MaVi,
    int MaDanhMuc,
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Số tiền phải lớn hơn 0")] decimal SoTien,
    LoaiGiaoDich Loai,
    DateTime? NgayGiaoDich,
    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")] string? GhiChu,
    HoaDonRequest? HoaDon = null);

/// <param name="NgayGiaoDich">Bỏ trống thì giữ nguyên ngày cũ.</param>
public record SuaGiaoDichRequest(
    int MaVi,
    int MaDanhMuc,
    [Range(typeof(decimal), "0.01", "9999999999999999", ErrorMessage = "Số tiền phải lớn hơn 0")] decimal SoTien,
    LoaiGiaoDich Loai,
    DateTime? NgayGiaoDich,
    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")] string? GhiChu);

public record GiaoDichDto(
    int MaGiaoDich, decimal SoTien, LoaiGiaoDich Loai, DateTime NgayGiaoDich, string? GhiChu,
    int MaVi, string TenVi, int MaDanhMuc, string TenDanhMuc, bool CoHoaDon);

public record ChiTietHoaDonDto(string TenMatHang, decimal SoLuong, decimal DonGia, decimal ThanhTien);

public record HoaDonDto(
    int MaHoaDon, string? TenCuaHang, DateTime NgayHoaDon, decimal TongTien, string? AnhHoaDon,
    List<ChiTietHoaDonDto> ChiTiet);

public record GiaoDichChiTietDto(
    int MaGiaoDich, decimal SoTien, LoaiGiaoDich Loai, DateTime NgayGiaoDich, string? GhiChu,
    int MaVi, string TenVi, int MaDanhMuc, string TenDanhMuc, bool CoHoaDon, HoaDonDto? HoaDon);

public record TrangKetQua<T>(List<T> Muc, int Trang, int TongSo);
