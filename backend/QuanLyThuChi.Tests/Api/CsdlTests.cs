using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Api;

public class CsdlTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void KhoiTao_TaoDu12DanhMuc()
    {
        var ds = factory.VoiDb(db => db.DanhMuc.Where(d => d.MaTaiKhoan == null).ToList());

        ds.Should().HaveCount(12);
        ds.Count(d => d.Loai == LoaiGiaoDich.Chi && !d.LaHeThong).Should().Be(8);
        ds.Count(d => d.Loai == LoaiGiaoDich.Thu).Should().Be(3);
        ds.Should().OnlyContain(d => d.LaMacDinh);
        var tietKiem = ds.Single(d => d.TenDanhMuc == "Tiết kiệm");
        tietKiem.LaHeThong.Should().BeTrue();
        tietKiem.Loai.Should().Be(LoaiGiaoDich.Chi);
    }

    [Fact]
    public async Task KhoiTao_ChayHaiLan_KhongTrung()
    {
        using var scope = factory.TaoScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bam = scope.ServiceProvider.GetRequiredService<IBamMatKhau>();

        await DuLieuKhoiTao.NapAsync(db, bam);
        await DuLieuKhoiTao.NapAsync(db, bam);

        (await db.DanhMuc.CountAsync()).Should().Be(12);
        (await db.TaiKhoan.CountAsync(t => t.Email == "admin@demo.vn")).Should().Be(1);
    }

    [Fact]
    public void KhoiTao_TaoAdminDemo_MatKhauDungHash()
    {
        using var scope = factory.TaoScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bam = scope.ServiceProvider.GetRequiredService<IBamMatKhau>();

        var admin = db.TaiKhoan.OfType<QuanTriVien>().Single(t => t.Email == "admin@demo.vn");

        admin.HoTen.Should().Be("Quản trị viên");
        admin.TrangThai.Should().Be(TrangThaiTaiKhoan.HoatDong);
        admin.MatKhauHash.Should().NotBe("Demo@123");
        bam.KiemTra("Demo@123", admin.MatKhauHash).Should().BeTrue();
        bam.KiemTra("sai", admin.MatKhauHash).Should().BeFalse();
    }

    [Fact]
    public void NganSach_TrungThangDanhMuc_ViPhamUnique()
    {
        using var scope = factory.TaoScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dongHo = scope.ServiceProvider.GetRequiredService<IDongHo>();
        var bam = scope.ServiceProvider.GetRequiredService<IBamMatKhau>();

        var nd = new NguoiDung("Nguyễn Văn A", "unique@test.vn", bam.Bam("Abc@1234"), dongHo.BayGio);
        db.TaiKhoan.Add(nd);
        db.SaveChanges();
        var dm = db.DanhMuc.First(d => d.MaTaiKhoan == null && d.Loai == LoaiGiaoDich.Chi && !d.LaHeThong);

        db.NganSach.Add(new NganSach(nd.MaTaiKhoan, dm.MaDanhMuc, 1_000_000m, 11, 2026));
        db.SaveChanges();

        db.NganSach.Add(new NganSach(nd.MaTaiKhoan, dm.MaDanhMuc, 2_000_000m, 11, 2026));
        var act = () => db.SaveChanges();

        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void XoaNguoiDung_CascadeCacBangCon()
    {
        using var scope = factory.TaoScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dongHo = scope.ServiceProvider.GetRequiredService<IDongHo>();

        var nd = new NguoiDung("Cascade", "cascade@test.vn", "hash", dongHo.BayGio);
        db.TaiKhoan.Add(nd);
        db.SaveChanges();
        var vi = new Vi(nd.MaTaiKhoan, "Ví", LoaiVi.TienMat, 100m, dongHo.BayGio);
        db.Vi.Add(vi);
        db.ThongBao.Add(new ThongBao(nd.MaTaiKhoan, LoaiThongBao.NhacGhiChep, "nhắc", dongHo.BayGio));
        db.SaveChanges();

        db.TaiKhoan.Remove(nd);
        db.SaveChanges();

        db.Vi.Count(v => v.MaTaiKhoan == nd.MaTaiKhoan).Should().Be(0);
        db.ThongBao.Count(t => t.MaTaiKhoan == nd.MaTaiKhoan).Should().Be(0);
        db.CaiDatNhacNho.Count().Should().Be(0);
    }
}
