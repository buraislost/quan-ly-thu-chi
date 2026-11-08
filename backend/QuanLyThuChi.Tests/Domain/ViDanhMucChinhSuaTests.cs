using FluentAssertions;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Domain;

public class ViDanhMucChinhSuaTests
{
    private static readonly DateTime Ngay = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Vi_DoiThongTin_DoiTenVaLoai_GiuSoDu()
    {
        var vi = new Vi(1, "Cũ", LoaiVi.TienMat, 500, Ngay);
        vi.DoiThongTin("  Mới ", LoaiVi.NganHang);
        vi.TenVi.Should().Be("Mới");
        vi.LoaiVi.Should().Be(LoaiVi.NganHang);
        vi.SoDu.Should().Be(500);
    }

    [Fact]
    public void Vi_DoiThongTin_TenTrong_NemLoi()
    {
        var vi = new Vi(1, "Cũ", LoaiVi.TienMat, 0, Ngay);
        var act = () => vi.DoiThongTin(" ", LoaiVi.TienMat);
        act.Should().Throw<DomainException>().WithMessage("Tên ví không được để trống");
        vi.TenVi.Should().Be("Cũ");
    }

    [Fact]
    public void DanhMuc_DoiThongTin_DoiTenBieuTuongMau_GiuLoai()
    {
        var dm = new DanhMuc(1, "Cũ", LoaiGiaoDich.Chi, "a", "#000", false, false);
        dm.DoiThongTin(" Mới ", "b", "#fff");
        dm.TenDanhMuc.Should().Be("Mới");
        dm.BieuTuong.Should().Be("b");
        dm.MauSac.Should().Be("#fff");
        dm.Loai.Should().Be(LoaiGiaoDich.Chi);
    }

    [Fact]
    public void DanhMuc_DoiThongTin_TenTrong_NemLoi()
    {
        var dm = new DanhMuc(1, "Cũ", LoaiGiaoDich.Chi, null, null, false, false);
        var act = () => dm.DoiThongTin("", null, null);
        act.Should().Throw<DomainException>().WithMessage("Tên danh mục không được để trống");
        dm.TenDanhMuc.Should().Be("Cũ");
    }

    [Fact]
    public void GiaoDich_ChuyenDanhMuc_CungLoai_DoiDanhMucGiuViVaSoDu()
    {
        var vi = new Vi(1, "V", LoaiVi.TienMat, 1000, Ngay);
        var cu = new DanhMuc(1, "Cũ", LoaiGiaoDich.Chi, null, null, false, false);
        var moi = new DanhMuc(1, "Mới", LoaiGiaoDich.Chi, null, null, false, false);
        var gd = new GiaoDich(1, vi, cu, 100, LoaiGiaoDich.Chi, Ngay, null);
        gd.GhiNhan();

        gd.ChuyenDanhMuc(moi);

        gd.DanhMuc.Should().BeSameAs(moi);
        vi.SoDu.Should().Be(900);
    }

    [Fact]
    public void GiaoDich_ChuyenDanhMuc_KhacLoai_NemLoi()
    {
        var vi = new Vi(1, "V", LoaiVi.TienMat, 1000, Ngay);
        var chi = new DanhMuc(1, "Chi", LoaiGiaoDich.Chi, null, null, false, false);
        var thu = new DanhMuc(1, "Thu", LoaiGiaoDich.Thu, null, null, false, false);
        var gd = new GiaoDich(1, vi, chi, 100, LoaiGiaoDich.Chi, Ngay, null);

        var act = () => gd.ChuyenDanhMuc(thu);

        act.Should().Throw<DomainException>().WithMessage("Danh mục không đúng loại giao dịch");
        gd.DanhMuc.Should().BeSameAs(chi);
    }

    [Fact]
    public void NganSach_ChuyenDanhMuc_VaGopHanMuc()
    {
        var ns = new NganSach(1, 5, 300_000, 11, 2026);
        ns.ChuyenDanhMuc(9);
        ns.MaDanhMuc.Should().Be(9);
        ns.GopHanMuc(200_000);
        ns.HanMuc.Should().Be(500_000);
        var act = () => ns.GopHanMuc(0);
        act.Should().Throw<DomainException>().WithMessage("Hạn mức phải lớn hơn 0");
    }
}
