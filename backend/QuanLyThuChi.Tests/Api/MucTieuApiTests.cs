using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Api;

public class MucTieuApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private int MaDm(string ten, LoaiGiaoDich loai) =>
        factory.VoiDb(db => db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.TenDanhMuc == ten && d.Loai == loai).MaDanhMuc);

    private decimal SoDu(int maVi) => factory.VoiDb(db => db.Vi.AsNoTracking().Single(v => v.MaVi == maVi).SoDu);

    private static async Task<int> MaTaiKhoan(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi")).GetProperty("maTaiKhoan").GetInt32();

    /// <summary>Người dùng mới, ví tiền mặt có sẵn <paramref name="soDu"/>.</summary>
    private async Task<(HttpClient C, int Tk, int Vi)> NguoiDung(decimal soDu = 1_000_000)
    {
        var c = await factory.TaoClientDaDangNhap(EmailMoi());
        var vi = (await c.GetFromJsonAsync<JsonElement>("/api/vi")).EnumerateArray().First().GetProperty("maVi").GetInt32();
        if (soDu > 0)
        {
            var r = await c.PostAsJsonAsync("/api/giao-dich", new
            {
                maVi = vi, maDanhMuc = MaDm("Lương", LoaiGiaoDich.Thu), soTien = soDu, loai = "Thu",
            });
            r.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        return (c, await MaTaiKhoan(c), vi);
    }

    private static Task<HttpResponseMessage> TaoMucTieu(HttpClient c, string ten, decimal soTien, DateOnly hanChot) =>
        c.PostAsJsonAsync("/api/muc-tieu", new { tenMucTieu = ten, soTienMucTieu = soTien, hanChot = hanChot.ToString("yyyy-MM-dd") });

    private async Task<int> TaoThanhCong(HttpClient c, string ten = "Mua laptop", decimal soTien = 500_000, DateOnly? han = null)
    {
        var r = await TaoMucTieu(c, ten, soTien, han ?? factory.DongHo.HomNay.AddMonths(5));
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maMucTieu").GetInt32();
    }

    private static Task<HttpResponseMessage> Nap(HttpClient c, int id, int vi, decimal soTien) =>
        c.PostAsJsonAsync($"/api/muc-tieu/{id}/nap-tien", new { maVi = vi, soTien });

    private static async Task<List<string>> ThongBao(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/thong-bao")).EnumerateArray()
        .Select(x => x.GetProperty("noiDung").GetString()!).ToList();

    [Fact]
    public async Task TaoMucTieu_HanChotQuaKhu_400()
    {
        var (c, _, _) = await NguoiDung(0);
        var r = await TaoMucTieu(c, "Du lịch", 1_000_000, factory.DongHo.HomNay);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Hạn chót phải sau hôm nay");
    }

    [Fact]
    public async Task TaoMucTieu_TraVeDto()
    {
        var (c, _, _) = await NguoiDung(0);
        var han = factory.DongHo.HomNay.AddMonths(4);
        var r = await TaoMucTieu(c, "Du lịch", 1_000_000, han);

        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        j.GetProperty("tenMucTieu").GetString().Should().Be("Du lịch");
        j.GetProperty("soTienHienTai").GetDecimal().Should().Be(0);
        j.GetProperty("tienDo").GetDouble().Should().Be(0);
        j.GetProperty("soTienCanMoiThang").GetDecimal().Should().Be(250_000);
        j.GetProperty("daHoanThanh").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task NapTien_TruViTaoGiaoDichTietKiem()
    {
        var (c, tk, vi) = await NguoiDung();
        var id = await TaoThanhCong(c, soTien: 500_000);

        var r = await Nap(c, id, vi, 200_000);

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        j.GetProperty("soTienHienTai").GetDecimal().Should().Be(200_000);
        j.GetProperty("tienDo").GetDouble().Should().Be(40.0);
        j.GetProperty("daHoanThanh").GetBoolean().Should().BeFalse();
        SoDu(vi).Should().Be(800_000);
        var gd = factory.VoiDb(db => db.GiaoDich.Include(g => g.DanhMuc).Single(g => g.MaTaiKhoan == tk && g.MaMucTieu == id));
        gd.Loai.Should().Be(LoaiGiaoDich.Chi);
        gd.SoTien.Should().Be(200_000);
        gd.DanhMuc.TenDanhMuc.Should().Be("Tiết kiệm");
        gd.GhiChu.Should().Be("Nạp mục tiêu: Mua laptop");
    }

    [Fact]
    public async Task NapTien_ViKhongDu_400VaMucTieuKhongDoi()
    {
        var (c, tk, vi) = await NguoiDung(100_000);
        var id = await TaoThanhCong(c);

        var r = await Nap(c, id, vi, 100_001);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Số dư ví không đủ");
        SoDu(vi).Should().Be(100_000);
        factory.VoiDb(db => db.MucTieuTietKiem.AsNoTracking().Single(m => m.MaMucTieu == id).SoTienHienTai).Should().Be(0);
        factory.VoiDb(db => db.GiaoDich.Count(g => g.MaMucTieu == id)).Should().Be(0);
    }

    [Fact]
    public async Task NapTien_DuMucTieu_ThongBaoMotLan()
    {
        var (c, _, vi) = await NguoiDung();
        var id = await TaoThanhCong(c, "Mua laptop", 300_000);

        (await Nap(c, id, vi, 300_000)).StatusCode.Should().Be(HttpStatusCode.OK);
        var sau = await ThongBao(c);
        sau.Count(n => n == "Chúc mừng! Bạn đã hoàn thành mục tiêu Mua laptop").Should().Be(1);

        var r = await Nap(c, id, vi, 50_000);
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("daHoanThanh").GetBoolean().Should().BeTrue();
        (await ThongBao(c)).Count(n => n.StartsWith("Chúc mừng!")).Should().Be(1);
    }

    [Fact]
    public async Task LietKe_ChuaXongTruocRoiTheoHanChot()
    {
        var (c, _, vi) = await NguoiDung();
        var hn = factory.DongHo.HomNay;
        var xong = await TaoThanhCong(c, "Xong", 100_000, hn.AddMonths(1));
        var muon = await TaoThanhCong(c, "Muộn", 100_000, hn.AddMonths(6));
        var som = await TaoThanhCong(c, "Sớm", 100_000, hn.AddMonths(2));
        (await Nap(c, xong, vi, 100_000)).StatusCode.Should().Be(HttpStatusCode.OK);

        var ds = (await c.GetFromJsonAsync<JsonElement>("/api/muc-tieu")).EnumerateArray()
            .Select(x => x.GetProperty("tenMucTieu").GetString()).ToList();

        ds.Should().Equal("Sớm", "Muộn", "Xong");
    }

    [Fact]
    public async Task Sua_DoiThongTin_VaHanChotQuaKhu400()
    {
        var (c, _, _) = await NguoiDung(0);
        var id = await TaoThanhCong(c);
        var han = factory.DongHo.HomNay.AddMonths(8).ToString("yyyy-MM-dd");

        var ok = await c.PutAsJsonAsync($"/api/muc-tieu/{id}", new { tenMucTieu = "Mua xe", soTienMucTieu = 900_000, hanChot = han });
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        var j = await ok.Content.ReadFromJsonAsync<JsonElement>();
        j.GetProperty("tenMucTieu").GetString().Should().Be("Mua xe");
        j.GetProperty("soTienMucTieu").GetDecimal().Should().Be(900_000);

        var xau = await c.PutAsJsonAsync($"/api/muc-tieu/{id}", new
        {
            tenMucTieu = "Mua xe", soTienMucTieu = 900_000, hanChot = factory.DongHo.HomNay.ToString("yyyy-MM-dd"),
        });
        xau.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(xau)).Should().Be("Hạn chót phải sau hôm nay");
    }

    [Fact]
    public async Task Xoa_KhongCoTienNap_204_ConCoTienNap_400()
    {
        var (c, _, vi) = await NguoiDung();
        var trong = await TaoThanhCong(c, "Trống");
        var coTien = await TaoThanhCong(c, "Có tiền");
        (await Nap(c, coTien, vi, 10_000)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await c.DeleteAsync($"/api/muc-tieu/{trong}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var r = await c.DeleteAsync($"/api/muc-tieu/{coTien}");
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Mục tiêu đã có tiền nạp, không thể xóa");
    }

    [Fact]
    public async Task MucTieuCuaNguoiKhac_404()
    {
        var (a, _, _) = await NguoiDung(0);
        var (b, _, viB) = await NguoiDung();
        var id = await TaoThanhCong(a);

        (await b.DeleteAsync($"/api/muc-tieu/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Nap(b, id, viB, 1000)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
