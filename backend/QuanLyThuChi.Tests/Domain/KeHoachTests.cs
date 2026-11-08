using System.Reflection;
using FluentAssertions;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Domain;

public class KeHoachTests
{
    private static readonly DateTime Ngay = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);

    private static NganSach TaoNganSach(decimal hanMuc = 1_000_000, int nguong = 80, int maDanhMuc = 5)
    {
        var ns = new NganSach(1, maDanhMuc, hanMuc, 11, 2026, nguong);
        return ns;
    }

    private static DanhMuc TaoDanhMuc(int ma, LoaiGiaoDich loai)
    {
        var dm = new DanhMuc(1, "Danh mục", loai, null, null, false, false);
        typeof(DanhMuc).GetProperty(nameof(DanhMuc.MaDanhMuc))!.SetValue(dm, ma);
        return dm;
    }

    private static GiaoDich TaoGiaoDich(int maDanhMuc, LoaiGiaoDich loai, decimal soTien)
    {
        var vi = new Vi(1, "Ví", LoaiVi.TienMat, 100_000_000, Ngay);
        return new GiaoDich(1, vi, TaoDanhMuc(maDanhMuc, loai), soTien, loai, Ngay, null);
    }

    private static MucTieuTietKiem TaoMucTieu(decimal muc = 10_000_000, DateOnly? ngayTao = null, DateOnly? hanChot = null)
        => new(1, "Mua laptop", muc, hanChot ?? new DateOnly(2026, 12, 31), ngayTao ?? new DateOnly(2026, 9, 1));

    [Fact]
    public void KiemTraVuot_85PhanTram_DatNguongMotLan()
    {
        var ns = TaoNganSach();
        ns.KiemTraVuot(850_000).Should().Be(KetQuaCanhBao.DatNguong);
        ns.KiemTraVuot(850_000).Should().Be(KetQuaCanhBao.KhongCo);
        ns.DaCanhBaoNguong.Should().BeTrue();
        ns.DaCanhBaoVuot.Should().BeFalse();
    }

    [Fact]
    public void KiemTraVuot_TuDuoiNguongLen120_TraVuot()
    {
        var ns = TaoNganSach();
        ns.KiemTraVuot(500_000).Should().Be(KetQuaCanhBao.KhongCo);
        ns.KiemTraVuot(1_200_000).Should().Be(KetQuaCanhBao.Vuot);
        ns.KiemTraVuot(1_300_000).Should().Be(KetQuaCanhBao.KhongCo);
        ns.DaCanhBaoNguong.Should().BeTrue();
        ns.DaCanhBaoVuot.Should().BeTrue();
    }

    [Fact]
    public void KiemTraVuot_DungBang100PhanTram_LaDatNguongKhongPhaiVuot()
    {
        var ns = TaoNganSach();
        ns.KiemTraVuot(1_000_000).Should().Be(KetQuaCanhBao.DatNguong);
    }

    [Fact]
    public void CapNhatHanMuc_TangHanMuc_ReSetCo()
    {
        var ns = TaoNganSach();
        ns.KiemTraVuot(1_200_000).Should().Be(KetQuaCanhBao.Vuot);

        ns.CapNhatHanMuc(2_000_000, 1_200_000); // 60% -> dưới ngưỡng
        ns.HanMuc.Should().Be(2_000_000);
        ns.DaCanhBaoNguong.Should().BeFalse();
        ns.DaCanhBaoVuot.Should().BeFalse();

        ns.KiemTraVuot(1_700_000).Should().Be(KetQuaCanhBao.DatNguong); // cảnh báo lại
    }

    [Fact]
    public void CapNhatHanMuc_VanConTrenNguong_GiuCoNguong()
    {
        var ns = TaoNganSach();
        ns.KiemTraVuot(1_200_000);
        ns.CapNhatHanMuc(1_300_000, 1_200_000); // ~92%: vẫn ≥ ngưỡng, không còn vượt
        ns.DaCanhBaoNguong.Should().BeTrue();
        ns.DaCanhBaoVuot.Should().BeFalse();
    }

    [Fact]
    public void TinhTienDo_TraPhanTram()
    {
        TaoNganSach().TinhTienDo(250_000).Should().BeApproximately(25.0, 1e-9);
    }

    [Fact]
    public void TinhSoDaChi_ChiTinhChiCungDanhMuc()
    {
        var ns = TaoNganSach(maDanhMuc: 5);
        var ds = new[]
        {
            TaoGiaoDich(5, LoaiGiaoDich.Chi, 100_000),
            TaoGiaoDich(5, LoaiGiaoDich.Chi, 50_000),
            TaoGiaoDich(6, LoaiGiaoDich.Chi, 999_000),
            TaoGiaoDich(5, LoaiGiaoDich.Thu, 777_000),
        };
        ns.TinhSoDaChi(ds).Should().Be(150_000);
    }

    [Fact]
    public void NganSach_GiaTriKhongHopLe_NemLoi()
    {
        var a = () => new NganSach(1, 1, 0, 11, 2026);
        a.Should().Throw<DomainException>().WithMessage("Hạn mức phải lớn hơn 0");
        var b = () => new NganSach(1, 1, 100, 13, 2026);
        b.Should().Throw<DomainException>().WithMessage("Tháng không hợp lệ");
        var c = () => new NganSach(1, 1, 100, 11, 2026, 101);
        c.Should().Throw<DomainException>().WithMessage("Ngưỡng cảnh báo phải từ 1 đến 100");
        var d = () => new NganSach(1, 1, 100, 11, 2026, 0);
        d.Should().Throw<DomainException>().WithMessage("Ngưỡng cảnh báo phải từ 1 đến 100");
    }

    [Fact]
    public void NapTien_DuMucTieu_TraTrueMotLanDuyNhat()
    {
        var mt = TaoMucTieu();
        mt.NapTien(4_000_000).Should().BeFalse();
        mt.NapTien(6_000_000).Should().BeTrue();
        mt.DaHoanThanh().Should().BeTrue();
        mt.NapTien(1_000_000).Should().BeFalse();
        mt.SoTienHienTai.Should().Be(11_000_000);
    }

    [Fact]
    public void NapTien_KhongHopLe_NemLoi()
    {
        var act = () => TaoMucTieu().NapTien(0);
        act.Should().Throw<DomainException>().WithMessage("Số tiền phải lớn hơn 0");
    }

    [Fact]
    public void TinhTienDo_MucTieu_TraPhanTram()
    {
        var mt = TaoMucTieu();
        mt.NapTien(2_500_000);
        mt.TinhTienDo().Should().BeApproximately(25.0, 1e-9);
    }

    [Fact]
    public void LaChamTienDo_QuaNuaThoiGianNapDuoiNua_True()
    {
        var mt = TaoMucTieu();
        mt.NapTien(3_000_000);
        mt.LaChamTienDo(new DateOnly(2026, 11, 15)).Should().BeTrue();
    }

    [Fact]
    public void LaChamTienDo_NapDuTheoKyVong_False()
    {
        var mt = TaoMucTieu();
        mt.NapTien(7_000_000);
        mt.LaChamTienDo(new DateOnly(2026, 11, 15)).Should().BeFalse();
    }

    [Fact]
    public void LaChamTienDo_DaHoanThanh_False_VaQuaHanChuaXong_True()
    {
        var xong = TaoMucTieu();
        xong.NapTien(10_000_000);
        xong.LaChamTienDo(new DateOnly(2027, 1, 5)).Should().BeFalse();

        var chua = TaoMucTieu();
        chua.NapTien(1);
        chua.LaChamTienDo(new DateOnly(2026, 12, 31)).Should().BeTrue();
    }

    [Fact]
    public void SoTienCanMoiThang_ConLai6Tr3Thang_2Tr()
    {
        var mt = TaoMucTieu();
        mt.NapTien(4_000_000);
        mt.SoTienCanMoiThang(new DateOnly(2026, 10, 1)).Should().Be(2_000_000);
    }

    [Fact]
    public void SoTienCanMoiThang_DaXongHoacQuaHan()
    {
        var xong = TaoMucTieu();
        xong.NapTien(12_000_000);
        xong.SoTienCanMoiThang(new DateOnly(2026, 10, 1)).Should().Be(0);

        var quaHan = TaoMucTieu();
        quaHan.NapTien(4_000_000);
        quaHan.SoTienCanMoiThang(new DateOnly(2027, 2, 1)).Should().Be(6_000_000); // tối thiểu 1 tháng
    }

    [Fact]
    public void MucTieu_KhongHopLe_NemLoi()
    {
        var a = () => new MucTieuTietKiem(1, " ", 100, new DateOnly(2026, 12, 31), new DateOnly(2026, 9, 1));
        a.Should().Throw<DomainException>().WithMessage("Tên mục tiêu không được để trống");
        var b = () => new MucTieuTietKiem(1, "x", 0, new DateOnly(2026, 12, 31), new DateOnly(2026, 9, 1));
        b.Should().Throw<DomainException>().WithMessage("Số tiền mục tiêu phải lớn hơn 0");
        var c = () => new MucTieuTietKiem(1, "x", 1, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1));
        c.Should().Throw<DomainException>().WithMessage("Hạn chót phải sau hôm nay");
    }

    [Fact]
    public void ThongBao_DanhDauDaDoc_VaNoiDungTrongNemLoi()
    {
        var tb = new ThongBao(1, LoaiThongBao.CanhBaoNganSach, "Nội dung", Ngay);
        tb.DaDoc.Should().BeFalse();
        tb.DanhDauDaDoc();
        tb.DaDoc.Should().BeTrue();

        var act = () => new ThongBao(1, LoaiThongBao.NhacGhiChep, " ", Ngay);
        act.Should().Throw<DomainException>().WithMessage("Nội dung thông báo không được để trống");
    }

    [Fact]
    public void LapBaoCao_GhiDeSoLieu()
    {
        var bc = new BaoCaoThang(1, 10, 2026);
        bc.LapBaoCao(10, 5, 1, Ngay);
        bc.LapBaoCao(20, 8, 2, Ngay.AddDays(1));
        bc.TongThu.Should().Be(20);
        bc.TongChi.Should().Be(8);
        bc.SoDanhMucVuot.Should().Be(2);
        bc.NgayLap.Should().Be(Ngay.AddDays(1));
    }
}
