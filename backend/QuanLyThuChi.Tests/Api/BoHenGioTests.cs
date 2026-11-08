using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Jobs;

namespace QuanLyThuChi.Tests.Api;

public class BoHenGioTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static DateTime Gio(int nam, int thang, int ngay, int gio, int phut) =>
        new(nam, thang, ngay, gio, phut, 0, DateTimeKind.Unspecified);

    private async Task<(HttpClient Client, int MaTk)> TaoNguoiDung(string gioNhac, bool nhacHangNgay = true, bool nhacMucTieu = true)
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var luu = await client.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay, gioNhac, nhacNganSach = true, nhacMucTieu });
        luu.EnsureSuccessStatusCode();
        var ma = (await client.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi")).GetProperty("maTaiKhoan").GetInt32();
        return (client, ma);
    }

    private int Dem(int maTk, LoaiThongBao loai) =>
        factory.VoiDb(db => db.ThongBao.Count(t => t.MaTaiKhoan == maTk && t.Loai == loai));

    private void ThemGiaoDich(int maTk, DateTime ngay) => factory.VoiDb(db =>
    {
        var vi = db.Vi.First(v => v.MaTaiKhoan == maTk);
        var dm = db.DanhMuc.First(d => d.MaTaiKhoan == null && d.Loai == LoaiGiaoDich.Chi);
        db.GiaoDich.Add(new GiaoDich(maTk, vi, dm, 50_000, LoaiGiaoDich.Chi, ngay, null));
        return db.SaveChanges();
    });

    private Task ChayNhac(DateTime bayGio)
    {
        factory.DongHo.BayGio = bayGio;
        return factory.Services.GetRequiredService<NhacNhoHangNgayJob>().ChayMotLanAsync(CancellationToken.None);
    }

    private Task ChayTongKet(DateTime bayGio)
    {
        factory.DongHo.BayGio = bayGio;
        return factory.Services.GetRequiredService<TongKetThangJob>().ChayMotLanAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Nhac_DungGio_ChuaGhiChep_TaoThongBao()
    {
        var (_, ma) = await TaoNguoiDung("21:30");

        await ChayNhac(Gio(2026, 11, 8, 21, 30));

        Dem(ma, LoaiThongBao.NhacGhiChep).Should().Be(1);
        factory.VoiDb(db => db.ThongBao.Single(t => t.MaTaiKhoan == ma && t.Loai == LoaiThongBao.NhacGhiChep).NoiDung)
            .Should().Be("Hôm nay bạn chưa ghi chép chi tiêu nào. Dành 1 phút ghi lại nhé!");
    }

    [Fact]
    public async Task Nhac_SaiGio_KhongTao()
    {
        var (_, ma) = await TaoNguoiDung("21:30");

        await ChayNhac(Gio(2026, 11, 8, 21, 31));

        Dem(ma, LoaiThongBao.NhacGhiChep).Should().Be(0);
    }

    [Fact]
    public async Task Nhac_DaCoGiaoDichHomNay_KhongTao()
    {
        var (_, ma) = await TaoNguoiDung("21:30");
        ThemGiaoDich(ma, Gio(2026, 11, 8, 8, 0));

        await ChayNhac(Gio(2026, 11, 8, 21, 30));

        Dem(ma, LoaiThongBao.NhacGhiChep).Should().Be(0);
    }

    [Fact]
    public async Task Nhac_ChayHaiLanCungNgay_MotThongBao()
    {
        var (_, ma) = await TaoNguoiDung("21:30");

        await ChayNhac(Gio(2026, 11, 8, 21, 30));
        await ChayNhac(Gio(2026, 11, 8, 21, 30));

        Dem(ma, LoaiThongBao.NhacGhiChep).Should().Be(1);
    }

    [Fact]
    public async Task Nhac_TatNhacHangNgay_KhongTao()
    {
        var (_, ma) = await TaoNguoiDung("21:30", nhacHangNgay: false);

        await ChayNhac(Gio(2026, 11, 8, 21, 30));

        Dem(ma, LoaiThongBao.NhacGhiChep).Should().Be(0);
    }

    [Fact]
    public async Task Nhac_TaiKhoanBiKhoa_KhongTao()
    {
        var email = EmailMoi();
        var client = await factory.TaoClientDaDangNhap(email);
        (await client.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay = true, gioNhac = "21:30", nhacNganSach = true, nhacMucTieu = true })).EnsureSuccessStatusCode();
        var ma = factory.VoiDb(db =>
        {
            var admin = (QuanTriVien)db.TaiKhoan.Single(t => t.Email == QuanLyThuChi.Data.DuLieuKhoiTao.EmailAdmin);
            var tk = db.TaiKhoan.Single(t => t.Email == email);
            admin.KhoaTaiKhoan(tk, "Vi phạm");
            db.SaveChanges();
            return tk.MaTaiKhoan;
        });

        await ChayNhac(Gio(2026, 11, 8, 21, 30));

        Dem(ma, LoaiThongBao.NhacGhiChep).Should().Be(0);
    }

    [Fact]
    public async Task Nhac_MucTieuCham_TaoNhacMucTieu()
    {
        var (_, ma) = await TaoNguoiDung("21:30");
        // Tạo ngày 1/9, hạn 31/12, chưa nạp đồng nào: đến 8/11 chắc chắn chậm.
        factory.VoiDb(db =>
        {
            db.MucTieuTietKiem.Add(new MucTieuTietKiem(ma, "Mua xe", 12_000_000, new DateOnly(2026, 12, 31), new DateOnly(2026, 9, 1)));
            return db.SaveChanges();
        });

        await ChayNhac(Gio(2026, 11, 8, 21, 30));
        await ChayNhac(Gio(2026, 11, 8, 21, 30));

        Dem(ma, LoaiThongBao.NhacMucTieu).Should().Be(1);
        factory.VoiDb(db => db.ThongBao.Single(t => t.MaTaiKhoan == ma && t.Loai == LoaiThongBao.NhacMucTieu).NoiDung)
            .Should().StartWith("Mục tiêu \"Mua xe\" đang chậm tiến độ, nên nạp thêm ").And.EndWith("đ mỗi tháng");
    }

    [Fact]
    public async Task Nhac_MucTieuKhongCham_KhongTaoNhacMucTieu()
    {
        var (_, ma) = await TaoNguoiDung("21:30");
        factory.VoiDb(db =>
        {
            var mt = new MucTieuTietKiem(ma, "Mua xe", 12_000_000, new DateOnly(2026, 12, 31), new DateOnly(2026, 9, 1));
            mt.NapTien(12_000_000);
            db.MucTieuTietKiem.Add(mt);
            return db.SaveChanges();
        });

        await ChayNhac(Gio(2026, 11, 8, 21, 30));

        Dem(ma, LoaiThongBao.NhacMucTieu).Should().Be(0);
    }

    [Fact]
    public async Task TongKet_Ngay30_11Luc23h_TaoBaoCao()
    {
        var (_, ma) = await TaoNguoiDung("21:30");
        ThemGiaoDich(ma, Gio(2026, 11, 10, 8, 0));

        await ChayTongKet(Gio(2026, 11, 30, 23, 15));

        factory.VoiDb(db => db.BaoCaoThang.Count(b => b.MaTaiKhoan == ma && b.Thang == 11 && b.Nam == 2026)).Should().Be(1);
        Dem(ma, LoaiThongBao.TongKetThang).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TongKet_ChayLai_KhongNhanDoi()
    {
        var (_, ma) = await TaoNguoiDung("21:30");

        await ChayTongKet(Gio(2026, 10, 31, 23, 0));
        await ChayTongKet(Gio(2026, 10, 31, 23, 40));

        factory.VoiDb(db => db.BaoCaoThang.Count(b => b.MaTaiKhoan == ma && b.Thang == 10 && b.Nam == 2026)).Should().Be(1);
    }

    [Fact]
    public async Task TongKet_Ngay29_11_KhongChay()
    {
        var (_, ma) = await TaoNguoiDung("21:30");

        await ChayTongKet(Gio(2026, 11, 29, 23, 15));

        factory.VoiDb(db => db.BaoCaoThang.Count(b => b.MaTaiKhoan == ma)).Should().Be(0);
    }

    [Fact]
    public async Task TongKet_NgayCuoiNhungChuaDen23h_KhongChay()
    {
        var (_, ma) = await TaoNguoiDung("21:30");

        await ChayTongKet(Gio(2026, 12, 31, 22, 59));

        factory.VoiDb(db => db.BaoCaoThang.Count(b => b.MaTaiKhoan == ma)).Should().Be(0);
    }
}
