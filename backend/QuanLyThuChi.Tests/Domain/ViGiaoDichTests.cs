using FluentAssertions;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Domain;

public class ViGiaoDichTests
{
    private static readonly DateTime Ngay = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);

    private static Vi TaoVi(decimal soDu, string ten = "Ví chính") => new(1, ten, LoaiVi.TienMat, soDu, Ngay);

    private static DanhMuc TaoDanhMuc(LoaiGiaoDich loai) => new(1, "Danh mục", loai, null, null, false, false);

    [Fact]
    public void TruTien_VuotSoDu_NemSoDuViKhongDu()
    {
        var vi = TaoVi(50_000);
        var act = () => vi.TruTien(60_000);
        act.Should().Throw<DomainException>().WithMessage("Số dư ví không đủ");
        vi.SoDu.Should().Be(50_000);
    }

    [Fact]
    public void CongTien_SoAm_NemLoi()
    {
        var act = () => TaoVi(0).CongTien(-1);
        act.Should().Throw<DomainException>().WithMessage("Số tiền phải lớn hơn 0");
    }

    [Fact]
    public void TruTien_BangKhong_NemLoi()
    {
        var act = () => TaoVi(10).TruTien(0);
        act.Should().Throw<DomainException>().WithMessage("Số tiền phải lớn hơn 0");
    }

    [Fact]
    public void KiemTraSoDu_TraDungTheoSoDu()
    {
        var vi = TaoVi(100);
        vi.KiemTraSoDu(100).Should().BeTrue();
        vi.KiemTraSoDu(101).Should().BeFalse();
    }

    [Fact]
    public void Vi_TenTrong_NemLoi()
    {
        var act = () => TaoVi(0, " ");
        act.Should().Throw<DomainException>().WithMessage("Tên ví không được để trống");
    }

    [Fact]
    public void GhiNhan_Chi100k_ViGiam100k()
    {
        var vi = TaoVi(500_000);
        var gd = new GiaoDich(1, vi, TaoDanhMuc(LoaiGiaoDich.Chi), 100_000, LoaiGiaoDich.Chi, Ngay, null);
        gd.GhiNhan();
        vi.SoDu.Should().Be(400_000);
    }

    [Fact]
    public void GhiNhan_Thu_ViTang()
    {
        var vi = TaoVi(0);
        new GiaoDich(1, vi, TaoDanhMuc(LoaiGiaoDich.Thu), 70_000, LoaiGiaoDich.Thu, Ngay, null).GhiNhan();
        vi.SoDu.Should().Be(70_000);
    }

    [Fact]
    public void Huy_GiaoDichThu_ViGiam()
    {
        var vi = TaoVi(0);
        var gd = new GiaoDich(1, vi, TaoDanhMuc(LoaiGiaoDich.Thu), 300_000, LoaiGiaoDich.Thu, Ngay, null);
        gd.GhiNhan();
        gd.Huy();
        vi.SoDu.Should().Be(0);
    }

    [Fact]
    public void Huy_GiaoDichThuDaTieuHet_NemSoDuViKhongDu()
    {
        var vi = TaoVi(0);
        var gd = new GiaoDich(1, vi, TaoDanhMuc(LoaiGiaoDich.Thu), 300_000, LoaiGiaoDich.Thu, Ngay, null);
        gd.GhiNhan();
        vi.TruTien(250_000);
        var act = () => gd.Huy();
        act.Should().Throw<DomainException>().WithMessage("Số dư ví không đủ");
    }

    [Fact]
    public void CapNhat_DoiViAsangB_AHoanTien_BBiTru()
    {
        var a = TaoVi(500_000, "A");
        var b = TaoVi(500_000, "B");
        var dm = TaoDanhMuc(LoaiGiaoDich.Chi);
        var gd = new GiaoDich(1, a, dm, 100_000, LoaiGiaoDich.Chi, Ngay, null);
        gd.GhiNhan();
        a.SoDu.Should().Be(400_000);

        gd.CapNhat(b, dm, 100_000, LoaiGiaoDich.Chi, Ngay, "đổi ví");

        a.SoDu.Should().Be(500_000);
        b.SoDu.Should().Be(400_000);
        gd.Vi.Should().BeSameAs(b);
        gd.GhiChu.Should().Be("đổi ví");
    }

    [Fact]
    public void CapNhat_DoiThuThanhChi_ViGiam2LanSoTien()
    {
        var vi = TaoVi(1_000_000);
        var thu = TaoDanhMuc(LoaiGiaoDich.Thu);
        var chi = TaoDanhMuc(LoaiGiaoDich.Chi);
        var gd = new GiaoDich(1, vi, thu, 100_000, LoaiGiaoDich.Thu, Ngay, null);
        gd.GhiNhan();
        vi.SoDu.Should().Be(1_100_000);

        gd.CapNhat(vi, chi, 100_000, LoaiGiaoDich.Chi, Ngay, null);

        vi.SoDu.Should().Be(900_000);
        gd.Loai.Should().Be(LoaiGiaoDich.Chi);
    }

    [Fact]
    public void CapNhat_ThuDaTieu_ChiDoiGhiChu_ThanhCongVaSoDuGiuNguyen()
    {
        var vi = TaoVi(0);
        var thu = new GiaoDich(1, vi, TaoDanhMuc(LoaiGiaoDich.Thu), 1_000_000, LoaiGiaoDich.Thu, Ngay, null);
        thu.GhiNhan();
        vi.TruTien(900_000);

        thu.CapNhat(vi, thu.DanhMuc, 1_000_000, LoaiGiaoDich.Thu, Ngay, "Lương tháng 11");

        vi.SoDu.Should().Be(100_000);
        thu.GhiChu.Should().Be("Lương tháng 11");
    }

    [Fact]
    public void CapNhat_CungVi_ChiApPhanChenhLech()
    {
        var vi = TaoVi(0);
        var dmThu = TaoDanhMuc(LoaiGiaoDich.Thu);
        var thu = new GiaoDich(1, vi, dmThu, 1_000_000, LoaiGiaoDich.Thu, Ngay, null);
        thu.GhiNhan();
        vi.TruTien(900_000);

        thu.CapNhat(vi, dmThu, 1_500_000, LoaiGiaoDich.Thu, Ngay, null); // tăng khoản thu
        vi.SoDu.Should().Be(600_000);

        thu.CapNhat(vi, dmThu, 1_200_000, LoaiGiaoDich.Thu, Ngay, null); // giảm 300.000, ví còn đủ
        vi.SoDu.Should().Be(300_000);

        var act = () => thu.CapNhat(vi, dmThu, 200_000, LoaiGiaoDich.Thu, Ngay.AddDays(1), "mới"); // giảm 1.000.000
        act.Should().Throw<DomainException>().WithMessage("Số dư ví không đủ");
        vi.SoDu.Should().Be(300_000);
        thu.SoTien.Should().Be(1_200_000);
        thu.Ngay.Should().Be(Ngay);
        thu.GhiChu.Should().BeNull();
    }

    [Fact]
    public void CapNhat_ChiCungVi_TangSoTien_ChiTruPhanTang()
    {
        var vi = TaoVi(150_000);
        var dm = TaoDanhMuc(LoaiGiaoDich.Chi);
        var gd = new GiaoDich(1, vi, dm, 100_000, LoaiGiaoDich.Chi, Ngay, null);
        gd.GhiNhan();
        vi.SoDu.Should().Be(50_000);

        gd.CapNhat(vi, dm, 150_000, LoaiGiaoDich.Chi, Ngay, null);

        vi.SoDu.Should().Be(0);
        gd.SoTien.Should().Be(150_000);
    }

    [Fact]
    public void CapNhat_ViMoiKhongDu_NemLoiVaGiuNguyenTruongCu()
    {
        var a = TaoVi(500_000, "A");
        var b = TaoVi(10_000, "B");
        var dm = TaoDanhMuc(LoaiGiaoDich.Chi);
        var gd = new GiaoDich(1, a, dm, 100_000, LoaiGiaoDich.Chi, Ngay, "cũ");
        gd.GhiNhan();

        var act = () => gd.CapNhat(b, dm, 100_000, LoaiGiaoDich.Chi, Ngay.AddDays(1), "mới");

        act.Should().Throw<DomainException>().WithMessage("Số dư ví không đủ");
        gd.Vi.Should().BeSameAs(a);
        gd.SoTien.Should().Be(100_000);
        gd.Ngay.Should().Be(Ngay);
        gd.GhiChu.Should().Be("cũ");
        a.SoDu.Should().Be(400_000);
        b.SoDu.Should().Be(10_000);
    }

    [Fact]
    public void CapNhat_SoTienKhongHopLe_NemLoiVaKhongDoiVi()
    {
        var vi = TaoVi(500_000);
        var dm = TaoDanhMuc(LoaiGiaoDich.Chi);
        var gd = new GiaoDich(1, vi, dm, 100_000, LoaiGiaoDich.Chi, Ngay, null);
        gd.GhiNhan();
        var act = () => gd.CapNhat(vi, dm, 0, LoaiGiaoDich.Chi, Ngay, null);
        act.Should().Throw<DomainException>().WithMessage("Số tiền phải lớn hơn 0");
        vi.SoDu.Should().Be(400_000);
    }

    [Fact]
    public void GiaoDich_DanhMucKhacLoai_NemLoi()
    {
        var act = () => new GiaoDich(1, TaoVi(0), TaoDanhMuc(LoaiGiaoDich.Thu), 10_000, LoaiGiaoDich.Chi, Ngay, null);
        act.Should().Throw<DomainException>().WithMessage("Danh mục không đúng loại giao dịch");
    }

    [Fact]
    public void GiaoDich_SoTienKhongDuong_NemLoi()
    {
        var act = () => new GiaoDich(1, TaoVi(0), TaoDanhMuc(LoaiGiaoDich.Chi), 0, LoaiGiaoDich.Chi, Ngay, null);
        act.Should().Throw<DomainException>().WithMessage("Số tiền phải lớn hơn 0");
    }

    [Fact]
    public void HoaDon_TinhTongTien_BangTongThanhTien()
    {
        var hd = new HoaDon("Cửa hàng A", Ngay, null);
        hd.ThemChiTiet(new ChiTietHoaDon("Sữa", 2, 15_000));
        hd.ThemChiTiet(new ChiTietHoaDon("Bánh", 1, 20_000));
        hd.TinhTongTien().Should().Be(50_000);
        hd.TongTien.Should().Be(50_000);
    }

    [Fact]
    public void ChiTietHoaDon_SoLuongKhongHopLe_VaDonGiaAm_NemLoi()
    {
        var a = () => new ChiTietHoaDon("x", 0, 1);
        a.Should().Throw<DomainException>().WithMessage("Số lượng phải lớn hơn 0");
        var b = () => new ChiTietHoaDon("x", 1, -1);
        b.Should().Throw<DomainException>().WithMessage("Đơn giá không được âm");
    }

    [Fact]
    public void DanhMuc_LaCuaNguoiDung_VaDangDuocSuDung()
    {
        var rieng = new DanhMuc(7, "Cà phê", LoaiGiaoDich.Chi, null, null, false, false);
        var macDinh = new DanhMuc(null, "Ăn uống", LoaiGiaoDich.Chi, null, null, true, true);
        rieng.LaCuaNguoiDung(7).Should().BeTrue();
        rieng.LaCuaNguoiDung(8).Should().BeFalse();
        macDinh.LaCuaNguoiDung(7).Should().BeFalse();
        rieng.DangDuocSuDung(false, false).Should().BeFalse();
        rieng.DangDuocSuDung(true, false).Should().BeTrue();
        rieng.DangDuocSuDung(false, true).Should().BeTrue();
    }
}
