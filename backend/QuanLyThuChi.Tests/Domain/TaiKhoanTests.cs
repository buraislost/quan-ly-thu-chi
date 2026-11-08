using FluentAssertions;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Domain;

public class TaiKhoanTests
{
    private static readonly DateTime Ngay = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);

    private class BamGia : IBamMatKhau
    {
        public string Bam(string matKhau) => "hash:" + matKhau;
        public bool KiemTra(string matKhau, string hash) => hash == "hash:" + matKhau;
    }

    private static readonly BamGia Bam = new();

    private static NguoiDung TaoNguoiDung() =>
        new("Nguyễn Văn A", "a@example.com", Bam.Bam("matkhau1"), Ngay);

    private static QuanTriVien TaoQuanTri() =>
        new("Quản trị", "admin@example.com", Bam.Bam("admin123"), Ngay);

    [Fact]
    public void DangNhap_TaiKhoanBiKhoa_NemLoi()
    {
        var nd = TaoNguoiDung();
        TaoQuanTri().KhoaTaiKhoan(nd, "Vi phạm điều khoản");

        var act = () => nd.DangNhap("matkhau1", Bam);

        act.Should().Throw<DomainException>().WithMessage("Tài khoản đã bị khóa");
    }

    [Fact]
    public void DangNhap_SaiMatKhau_TraFalse()
    {
        TaoNguoiDung().DangNhap("sai", Bam).Should().BeFalse();
    }

    [Fact]
    public void DangNhap_DungMatKhau_TraTrue()
    {
        TaoNguoiDung().DangNhap("matkhau1", Bam).Should().BeTrue();
    }

    [Fact]
    public void DoiMatKhau_SaiMatKhauCu_TraFalseVaKhongDoiHash()
    {
        var nd = TaoNguoiDung();
        var hashCu = nd.MatKhauHash;

        nd.DoiMatKhau("sai", "matkhaumoi", Bam).Should().BeFalse();

        nd.MatKhauHash.Should().Be(hashCu);
    }

    [Fact]
    public void DoiMatKhau_DungMatKhauCu_DoiHash()
    {
        var nd = TaoNguoiDung();

        nd.DoiMatKhau("matkhau1", "matkhaumoi", Bam).Should().BeTrue();

        nd.MatKhauHash.Should().Be("hash:matkhaumoi");
    }

    [Fact]
    public void KhoaTaiKhoan_ChinhMinh_NemLoi()
    {
        var qt = TaoQuanTri();

        var act = () => qt.KhoaTaiKhoan(qt, "lý do");

        act.Should().Throw<DomainException>().WithMessage("Không thể tự khóa tài khoản của mình");
        qt.TrangThai.Should().Be(TrangThaiTaiKhoan.HoatDong);
    }

    [Fact]
    public void KhoaTaiKhoan_CungMaTaiKhoan_NemLoi()
    {
        var qt = TaoQuanTri();
        typeof(TaiKhoan).GetProperty(nameof(TaiKhoan.MaTaiKhoan))!.SetValue(qt, 7);
        var banSao = TaoQuanTri();
        typeof(TaiKhoan).GetProperty(nameof(TaiKhoan.MaTaiKhoan))!.SetValue(banSao, 7);

        var act = () => qt.KhoaTaiKhoan(banSao, "lý do");

        act.Should().Throw<DomainException>().WithMessage("Không thể tự khóa tài khoản của mình");
    }

    [Fact]
    public void KhoaTaiKhoan_LyDoTrong_NemLoi()
    {
        var act = () => TaoQuanTri().KhoaTaiKhoan(TaoNguoiDung(), "  ");

        act.Should().Throw<DomainException>().WithMessage("Vui lòng nhập lý do khóa");
    }

    [Fact]
    public void MoKhoaTaiKhoan_DatLaiHoatDong()
    {
        var nd = TaoNguoiDung();
        var qt = TaoQuanTri();
        qt.KhoaTaiKhoan(nd, "lý do");

        qt.MoKhoaTaiKhoan(nd);

        nd.TrangThai.Should().Be(TrangThaiTaiKhoan.HoatDong);
    }

    [Fact]
    public void CapNhatThongTin_DoiHoTen_VaTuChoiRong()
    {
        var nd = TaoNguoiDung();
        nd.CapNhatThongTin("Trần B");
        nd.HoTen.Should().Be("Trần B");

        var act = () => nd.CapNhatThongTin(" ");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void NguoiDung_MacDinh()
    {
        var nd = TaoNguoiDung();
        nd.DonViTienTe.Should().Be("VND");
        nd.TrangThai.Should().Be(TrangThaiTaiKhoan.HoatDong);
        nd.NgayTao.Should().Be(Ngay);
        nd.CaiDat.NhacHangNgay.Should().BeTrue();
    }

    [Fact]
    public void CaiDatNhacNho_KhoiPhucMacDinh_VeGiaTriMacDinh()
    {
        var cd = new CaiDatNhacNho
        {
            NhacHangNgay = false,
            GioNhac = new TimeOnly(7, 30),
            NhacNganSach = false,
            NhacMucTieu = false
        };

        cd.KhoiPhucMacDinh();

        cd.NhacHangNgay.Should().BeTrue();
        cd.GioNhac.Should().Be(new TimeOnly(20, 0));
        cd.NhacNganSach.Should().BeTrue();
        cd.NhacMucTieu.Should().BeTrue();
    }
}
