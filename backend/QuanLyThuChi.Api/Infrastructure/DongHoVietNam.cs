using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Infrastructure;

public class DongHoVietNam : IDongHo
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _mui;

    public DongHoVietNam(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _mui = TimNhom();
    }

    public DateTime BayGio
    {
        get
        {
            var gio = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _mui).DateTime;
            return DateTime.SpecifyKind(gio, DateTimeKind.Unspecified);
        }
    }

    public DateOnly HomNay => DateOnly.FromDateTime(BayGio);

    public DateTime SangGioVietNam(DateTime gio) => ChuyenSangGioVietNam(gio, _mui);

    /// <summary>Utc/Local được đổi sang giờ Việt Nam; Unspecified coi như đã là giờ Việt Nam. Kết quả luôn Unspecified.</summary>
    public static DateTime ChuyenSangGioVietNam(DateTime gio, TimeZoneInfo? mui = null)
    {
        if (gio.Kind == DateTimeKind.Unspecified) return gio;
        var vn = TimeZoneInfo.ConvertTime(gio, mui ?? TimNhom());
        return DateTime.SpecifyKind(vn, DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo TimNhom()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
