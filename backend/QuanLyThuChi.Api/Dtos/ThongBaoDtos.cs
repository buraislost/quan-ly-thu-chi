using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Dtos;

public record ThongBaoDto(int MaThongBao, LoaiThongBao Loai, string NoiDung, bool DaDoc, DateTime NgayTao);

public record SoChuaDocDto(int SoChuaDoc);

/// <summary>
/// Cài đặt nhắc nhở. GioNhac dạng "HH:mm" (24 giờ); kiểu string? để giờ thiếu/sai cùng trả một thông báo lỗi.
/// </summary>
public record CaiDatDto(bool NhacHangNgay, string? GioNhac, bool NhacNganSach, bool NhacMucTieu);
