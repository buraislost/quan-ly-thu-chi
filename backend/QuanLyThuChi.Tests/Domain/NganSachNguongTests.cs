using FluentAssertions;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Domain;

public class NganSachNguongTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void DoiNguong_NgoaiKhoang_Nem(int nguong)
    {
        var ns = new NganSach(1, 1, 1_000, 11, 2026);

        var act = () => ns.DoiNguong(nguong);

        act.Should().Throw<DomainException>().WithMessage("Ngưỡng cảnh báo phải từ 1 đến 100");
        ns.NguongCanhBao.Should().Be(80);
    }

    [Fact]
    public void DoiNguong_HopLe_DoiGiaTri()
    {
        var ns = new NganSach(1, 1, 1_000, 11, 2026);
        ns.DoiNguong(95);
        ns.NguongCanhBao.Should().Be(95);
    }
}
