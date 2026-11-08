using FluentAssertions;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Domain;

public class MucTieuDoiThongTinTests
{
    private static MucTieuTietKiem Tao() => new(1, "Mua laptop", 1_000_000, new DateOnly(2026, 12, 31), new DateOnly(2026, 9, 1));

    [Fact]
    public void DoiThongTin_CapNhatCacTruong()
    {
        var m = Tao();
        m.DoiThongTin("  Mua xe ", 2_000_000, new DateOnly(2027, 6, 1));
        m.TenMucTieu.Should().Be("Mua xe");
        m.SoTienMucTieu.Should().Be(2_000_000);
        m.HanChot.Should().Be(new DateOnly(2027, 6, 1));
    }

    [Fact]
    public void DoiThongTin_MucThapHonDaNap_DaHoanThanh()
    {
        var m = Tao();
        m.NapTien(600_000);
        m.DoiThongTin("Mua laptop", 500_000, new DateOnly(2026, 12, 31));
        m.DaHoanThanh().Should().BeTrue();
    }

    [Theory]
    [InlineData("", 100, "Tên mục tiêu không được để trống")]
    [InlineData("A", 0, "Số tiền mục tiêu phải lớn hơn 0")]
    public void DoiThongTin_KhongHopLe_Nem(string ten, decimal tien, string loi)
    {
        var m = Tao();
        var act = () => m.DoiThongTin(ten, tien, new DateOnly(2027, 1, 1));
        act.Should().Throw<DomainException>().WithMessage(loi);
    }

    [Fact]
    public void DoiThongTin_HanChotKhongSauNgayTao_Nem()
    {
        var m = Tao();
        var act = () => m.DoiThongTin("A", 100, new DateOnly(2026, 9, 1));
        act.Should().Throw<DomainException>().WithMessage("Hạn chót phải sau hôm nay");
    }
}
