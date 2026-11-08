using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Api;

public class DuLieuDemoTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;

    public DuLieuDemoTests(ApiFactory f)
    {
        _f = f;
        _f.DongHo.BayGio = new DateTime(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);
    }

    private async Task NapAsync()
    {
        using var scope = _f.TaoScope();
        var sp = scope.ServiceProvider;
        await DuLieuDemo.NapAsync(sp.GetRequiredService<AppDbContext>(), sp.GetRequiredService<IBamMatKhau>(), _f.DongHo);
    }

    private int MaDemo() => _f.VoiDb(db => db.TaiKhoan.Single(t => t.Email == DuLieuDemo.EmailDemo).MaTaiKhoan);

    [Fact]
    public async Task Demo_NapXong_SoDuViKhopTongGiaoDich()
    {
        await NapAsync();
        var ma = MaDemo();

        var dau = new Dictionary<string, decimal>
        {
            ["Tiền mặt"] = DuLieuDemo.SoDuDauTienMat,
            ["Vietcombank"] = DuLieuDemo.SoDuDauNganHang,
            ["MoMo"] = DuLieuDemo.SoDuDauViDienTu,
        };
        var vis = _f.VoiDb(db => db.Vi.AsNoTracking().Where(v => v.MaTaiKhoan == ma).ToList());
        vis.Select(v => v.TenVi).Should().BeEquivalentTo(dau.Keys);
        var gds = _f.VoiDb(db => db.GiaoDich.AsNoTracking().Where(g => g.MaTaiKhoan == ma).ToList());
        gds.Count.Should().BeInRange(85, 100);

        foreach (var vi in vis)
        {
            var thu = gds.Where(g => g.MaVi == vi.MaVi && g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien);
            var chi = gds.Where(g => g.MaVi == vi.MaVi && g.Loai == LoaiGiaoDich.Chi).Sum(g => g.SoTien);
            vi.SoDu.Should().Be(dau[vi.TenVi] + thu - chi, vi.TenVi);
            vi.SoDu.Should().BeGreaterThanOrEqualTo(0);
        }

        // Trải đều ba tháng gần nhất, không có giao dịch tương lai, lương mỗi tháng vào Vietcombank.
        gds.Select(g => (g.Ngay.Year, g.Ngay.Month)).Distinct().Should().BeEquivalentTo(new[] { (2026, 9), (2026, 10), (2026, 11) });
        gds.Should().OnlyContain(g => g.Ngay <= _f.DongHo.BayGio);
        var nganHang = vis.Single(v => v.TenVi == "Vietcombank").MaVi;
        gds.Where(g => g.Loai == LoaiGiaoDich.Thu).Should().HaveCount(3)
            .And.OnlyContain(g => g.SoTien == DuLieuDemo.Luong && g.MaVi == nganHang && g.Ngay.Day == 5);
    }

    [Fact]
    public async Task Demo_ChayHaiLan_KhongTrung()
    {
        await NapAsync();
        var truoc = _f.VoiDb(db => (db.TaiKhoan.Count(), db.Vi.Count(), db.GiaoDich.Count(), db.NganSach.Count(),
            db.MucTieuTietKiem.Count(), db.ThongBao.Count()));
        await NapAsync();
        var sau = _f.VoiDb(db => (db.TaiKhoan.Count(), db.Vi.Count(), db.GiaoDich.Count(), db.NganSach.Count(),
            db.MucTieuTietKiem.Count(), db.ThongBao.Count()));
        sau.Should().Be(truoc);
        _f.VoiDb(db => db.TaiKhoan.Count(t => t.Email == DuLieuDemo.EmailDemo)).Should().Be(1);
    }

    [Fact]
    public async Task Demo_NganSach_MotGanNguongMotVuot_CoThongBao()
    {
        await NapAsync();
        var ma = MaDemo();
        var homNay = _f.DongHo.HomNay;
        var (nganSach, ten) = _f.VoiDb(db => (
            db.NganSach.AsNoTracking().Where(n => n.MaTaiKhoan == ma).ToList(),
            db.DanhMuc.AsNoTracking().ToDictionary(d => d.MaDanhMuc, d => d.TenDanhMuc)));
        nganSach.Should().HaveCount(2).And.OnlyContain(n => n.Thang == homNay.Month && n.Nam == homNay.Year);
        var chiThang = _f.VoiDb(db => db.GiaoDich.AsNoTracking()
            .Where(g => g.MaTaiKhoan == ma && g.Loai == LoaiGiaoDich.Chi && g.Ngay >= new DateTime(2026, 11, 1)).ToList());

        var anUong = nganSach.Single(n => ten[n.MaDanhMuc] == "Ăn uống");
        var tienDoAn = anUong.TinhTienDo(anUong.TinhSoDaChi(chiThang));
        tienDoAn.Should().BeInRange(80, 90);
        anUong.DaCanhBaoNguong.Should().BeTrue();
        anUong.DaCanhBaoVuot.Should().BeFalse();

        var giaiTri = nganSach.Single(n => ten[n.MaDanhMuc] == "Giải trí");
        giaiTri.TinhTienDo(giaiTri.TinhSoDaChi(chiThang)).Should().BeGreaterThan(100);
        giaiTri.DaCanhBaoNguong.Should().BeTrue();
        giaiTri.DaCanhBaoVuot.Should().BeTrue();

        var thongBao = _f.VoiDb(db => db.ThongBao.AsNoTracking().Where(t => t.MaTaiKhoan == ma).ToList());
        thongBao.Should().HaveCount(2).And.OnlyContain(t => t.Loai == LoaiThongBao.CanhBaoNganSach);
        thongBao.Select(t => t.NoiDung).Should().Contain(s => s.Contains("Ăn uống") && s.Contains('%'))
            .And.Contain(s => s.Contains("Giải trí") && s.Contains("vượt hạn mức"));
    }

    [Fact]
    public async Task Demo_MucTieu_MotDungTienDoMotCham_TienNapLaGiaoDichTietKiem()
    {
        await NapAsync();
        var ma = MaDemo();
        var homNay = _f.DongHo.HomNay;
        var mucTieu = _f.VoiDb(db => db.MucTieuTietKiem.AsNoTracking().Where(m => m.MaTaiKhoan == ma).ToList());
        mucTieu.Should().HaveCount(2);

        var laptop = mucTieu.Single(m => m.TenMucTieu == "Mua laptop");
        laptop.SoTienMucTieu.Should().Be(20_000_000);
        laptop.LaChamTienDo(homNay).Should().BeFalse();
        var daLat = mucTieu.Single(m => m.TenMucTieu == "Du lịch Đà Lạt");
        daLat.SoTienMucTieu.Should().Be(8_000_000);
        daLat.LaChamTienDo(homNay).Should().BeTrue();

        var napTien = _f.VoiDb(db => db.GiaoDich.AsNoTracking().Include(g => g.DanhMuc)
            .Where(g => g.MaTaiKhoan == ma && g.MaMucTieu != null).ToList());
        napTien.Should().OnlyContain(g => g.DanhMuc.TenDanhMuc == "Tiết kiệm" && g.DanhMuc.LaHeThong && g.Loai == LoaiGiaoDich.Chi);
        foreach (var m in mucTieu)
            napTien.Where(g => g.MaMucTieu == m.MaMucTieu).Sum(g => g.SoTien).Should().Be(m.SoTienHienTai);
    }

    [Fact]
    public async Task Demo_DangNhapDuocBangMatKhauDemo()
    {
        await NapAsync();
        var client = await _f.TaoClientDaDangNhap(DuLieuDemo.EmailDemo, DuLieuDemo.MatKhauDemo);
        var res = await client.GetAsync("/api/bao-cao/tong-quan");
        res.EnsureSuccessStatusCode();
    }
}
