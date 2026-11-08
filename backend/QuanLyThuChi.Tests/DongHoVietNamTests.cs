using FluentAssertions;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Tests;

public class DongHoVietNamTests
{
    private sealed class TimeProviderCoDinh(DateTimeOffset utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utc;
    }

    [Fact]
    public void UtcLuc17h30Ngay31_10_LaNgay1_11GioVN()
    {
        var utc = new DateTimeOffset(2026, 10, 31, 17, 30, 0, TimeSpan.Zero);
        var dongHo = new DongHoVietNam(new TimeProviderCoDinh(utc));

        dongHo.BayGio.Should().Be(new DateTime(2026, 11, 1, 0, 30, 0));
        dongHo.BayGio.Kind.Should().Be(DateTimeKind.Unspecified);
        dongHo.HomNay.Should().Be(new DateOnly(2026, 11, 1));
    }
}
