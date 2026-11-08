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
