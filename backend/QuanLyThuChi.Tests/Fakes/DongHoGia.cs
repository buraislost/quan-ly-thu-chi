using QuanLyThuChi.Domain;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Tests.Fakes;

/// <summary>Đồng hồ giả cho test: giờ Việt Nam (Unspecified) do test tự đặt.</summary>
public class DongHoGia : IDongHo
{
    public DateTime BayGio { get; set; } = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);

    public DateOnly HomNay => DateOnly.FromDateTime(BayGio);

    public DateTime SangGioVietNam(DateTime gio) => DongHoVietNam.ChuyenSangGioVietNam(gio);
}
