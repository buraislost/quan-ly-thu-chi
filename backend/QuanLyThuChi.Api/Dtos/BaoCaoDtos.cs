namespace QuanLyThuChi.Dtos;

/// <param name="TyLe">Phần trăm trên tổng, làm tròn 1 chữ số thập phân.</param>
public record TheoDanhMucDto(int MaDanhMuc, string TenDanhMuc, decimal SoTien, double TyLe);

public record TheoThangDto(int Thang, decimal Thu, decimal Chi);

/// <param name="TongSoDu">Tổng số dư hiện tại của các ví, không gắn với tháng.</param>
/// <param name="SapVuot">Ngân sách của tháng đã dùng từ 80% hạn mức trở lên.</param>
/// <param name="GanDay">Tối đa 5 giao dịch gần nhất trong khoảng báo cáo.</param>
public record TongQuanDto(
    decimal TongThu, decimal TongChi, decimal ChenhLech, decimal TongSoDu,
    List<TheoDanhMucDto> ChiTheoDanhMuc, List<NganSachDto> SapVuot, List<GiaoDichDto> GanDay);

public record BaoCaoThangDto(int Thang, int Nam, decimal TongThu, decimal TongChi, int SoDanhMucVuot, DateTime NgayLap);
