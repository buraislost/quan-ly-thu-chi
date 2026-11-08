using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Tests.Api;

public class ThongBaoApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static async Task<int> MaTaiKhoan(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi")).GetProperty("maTaiKhoan").GetInt32();

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private int ThemThongBao(int maTk, LoaiThongBao loai, string noiDung, DateTime ngayTao) => factory.VoiDb(db =>
    {
        var tb = new ThongBao(maTk, loai, noiDung, ngayTao);
        db.ThongBao.Add(tb);
        db.SaveChanges();
        return tb.MaThongBao;
    });

    private static async Task<int> SoChuaDoc(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/thong-bao/so-chua-doc")).GetProperty("soChuaDoc").GetInt32();

    [Fact]
    public async Task LietKe_MoiNhatTruoc_VaLocChuaDoc()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var cu = ThemThongBao(maTk, LoaiThongBao.NhacGhiChep, "Cũ", new(2026, 11, 6, 20, 0, 0));
        var moi = ThemThongBao(maTk, LoaiThongBao.CanhBaoNganSach, "Mới", new(2026, 11, 8, 8, 0, 0));
        ThemThongBao(maTk, LoaiThongBao.NhacMucTieu, "Giữa", new(2026, 11, 7, 8, 0, 0));
        (await client.PutAsync($"/api/thong-bao/{cu}/da-doc", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var ds = await client.GetFromJsonAsync<JsonElement>("/api/thong-bao");
        ds.EnumerateArray().Select(x => x.GetProperty("noiDung").GetString()).Should().Equal("Mới", "Giữa", "Cũ");
        var dau = ds[0];
        dau.GetProperty("maThongBao").GetInt32().Should().Be(moi);
        dau.GetProperty("loai").GetString().Should().Be("CanhBaoNganSach");
        dau.GetProperty("daDoc").GetBoolean().Should().BeFalse();

        var chuaDoc = await client.GetFromJsonAsync<JsonElement>("/api/thong-bao?chuaDoc=true");
        chuaDoc.EnumerateArray().Select(x => x.GetProperty("noiDung").GetString()).Should().Equal("Mới", "Giữa");
    }

    [Fact]
    public async Task SoChuaDoc_SauKhiDanhDau_Giam()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var a = ThemThongBao(maTk, LoaiThongBao.NhacGhiChep, "A", new(2026, 11, 8, 8, 0, 0));
        ThemThongBao(maTk, LoaiThongBao.NhacGhiChep, "B", new(2026, 11, 8, 9, 0, 0));
        (await SoChuaDoc(client)).Should().Be(2);

        (await client.PutAsync($"/api/thong-bao/{a}/da-doc", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await SoChuaDoc(client)).Should().Be(1);
    }

    [Fact]
    public async Task DanhDauTatCa_DaDoc_204()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var khac = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        ThemThongBao(maTk, LoaiThongBao.NhacGhiChep, "A", new(2026, 11, 8, 8, 0, 0));
        ThemThongBao(maTk, LoaiThongBao.NhacGhiChep, "B", new(2026, 11, 8, 9, 0, 0));
        ThemThongBao(await MaTaiKhoan(khac), LoaiThongBao.NhacGhiChep, "Của người khác", new(2026, 11, 8, 9, 0, 0));

        (await client.PutAsync("/api/thong-bao/da-doc-tat-ca", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await SoChuaDoc(client)).Should().Be(0);
        (await SoChuaDoc(khac)).Should().Be(1);
    }

    [Fact]
    public async Task Xoa_CuaMinh_204()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = ThemThongBao(await MaTaiKhoan(client), LoaiThongBao.TongKetThang, "Tổng kết", new(2026, 11, 8, 8, 0, 0));

        (await client.DeleteAsync($"/api/thong-bao/{ma}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        factory.VoiDb(db => db.ThongBao.Any(t => t.MaThongBao == ma)).Should().BeFalse();
    }

    [Fact]
    public async Task XoaThongBaoNguoiKhac_404()
    {
        var a = await factory.TaoClientDaDangNhap(EmailMoi());
        var b = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = ThemThongBao(await MaTaiKhoan(a), LoaiThongBao.NhacGhiChep, "Của A", new(2026, 11, 8, 8, 0, 0));

        (await b.DeleteAsync($"/api/thong-bao/{ma}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await b.PutAsync($"/api/thong-bao/{ma}/da-doc", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await b.DeleteAsync("/api/thong-bao/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        factory.VoiDb(db => db.ThongBao.Single(t => t.MaThongBao == ma).DaDoc).Should().BeFalse();
        (await b.GetFromJsonAsync<JsonElement>("/api/thong-bao")).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ThongBao_KhongToken_401()
    {
        (await factory.CreateClient().GetAsync("/api/thong-bao")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await factory.CreateClient().GetAsync("/api/thong-bao/cai-dat")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ----- Service: Tao / DaGuiHomNay -----

    [Fact]
    public async Task Tao_ChiAdd_KhongSaveChanges_VaDungGioVietNam()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);

        using var scope = factory.TaoScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dich = scope.ServiceProvider.GetRequiredService<ThongBaoService>();
        dich.Tao(maTk, LoaiThongBao.NhacGhiChep, "Đừng quên ghi chép");

        factory.VoiDb(d => d.ThongBao.Count(t => t.MaTaiKhoan == maTk)).Should().Be(0);
        await db.SaveChangesAsync();
        factory.VoiDb(d => d.ThongBao.Single(t => t.MaTaiKhoan == maTk).NgayTao).Should().Be(factory.DongHo.BayGio);
    }

    [Fact]
    public async Task DaGuiHomNay_TheoNgayVietNamVaTheoLoai()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var homNay = factory.DongHo.HomNay;
        var homQua2359 = homNay.AddDays(-1).ToDateTime(new TimeOnly(23, 59));
        ThemThongBao(maTk, LoaiThongBao.NhacGhiChep, "Hôm qua", homQua2359);

        using (var scope = factory.TaoScope())
        {
            var dich = scope.ServiceProvider.GetRequiredService<ThongBaoService>();
            (await dich.DaGuiHomNayAsync(maTk, LoaiThongBao.NhacGhiChep)).Should().BeFalse();
        }

        ThemThongBao(maTk, LoaiThongBao.NhacGhiChep, "Hôm nay", homNay.ToDateTime(new TimeOnly(0, 0)));

        using (var scope = factory.TaoScope())
        {
            var dich = scope.ServiceProvider.GetRequiredService<ThongBaoService>();
            (await dich.DaGuiHomNayAsync(maTk, LoaiThongBao.NhacGhiChep)).Should().BeTrue();
            (await dich.DaGuiHomNayAsync(maTk, LoaiThongBao.CanhBaoNganSach)).Should().BeFalse();
            (await dich.DaGuiHomNayAsync(maTk + 100_000, LoaiThongBao.NhacGhiChep)).Should().BeFalse();
        }
    }

    // ----- Cài đặt nhắc nhở -----

    [Fact]
    public async Task LayCaiDat_MacDinh()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var json = await client.GetFromJsonAsync<JsonElement>("/api/thong-bao/cai-dat");

        json.GetProperty("nhacHangNgay").GetBoolean().Should().BeTrue();
        json.GetProperty("gioNhac").GetString().Should().Be("20:00");
        json.GetProperty("nhacNganSach").GetBoolean().Should().BeTrue();
        json.GetProperty("nhacMucTieu").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task LuuCaiDat_ThanhCong_VaDocLaiDuoc()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay = false, gioNhac = "07:30", nhacNganSach = false, nhacMucTieu = true });

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await client.GetFromJsonAsync<JsonElement>("/api/thong-bao/cai-dat");
        json.GetProperty("nhacHangNgay").GetBoolean().Should().BeFalse();
        json.GetProperty("gioNhac").GetString().Should().Be("07:30");
        json.GetProperty("nhacNganSach").GetBoolean().Should().BeFalse();
        json.GetProperty("nhacMucTieu").GetBoolean().Should().BeTrue();
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("7:30")]
    [InlineData("20:60")]
    [InlineData("abc")]
    [InlineData("08:30 PM")]
    [InlineData("")]
    public async Task LuuCaiDat_GioSaiDinhDang_400(string gio)
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay = true, gioNhac = gio, nhacNganSach = true, nhacMucTieu = true });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Giờ nhắc không hợp lệ (định dạng HH:mm)");
        (await client.GetFromJsonAsync<JsonElement>("/api/thong-bao/cai-dat")).GetProperty("gioNhac").GetString().Should().Be("20:00");
    }

    [Fact]
    public async Task LuuCaiDat_ThieuGio_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.PutAsJsonAsync("/api/thong-bao/cai-dat", new { nhacHangNgay = true, nhacNganSach = true, nhacMucTieu = true });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task KhoiPhucMacDinh_QuaPutGiaTriMacDinh()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        (await client.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay = false, gioNhac = "06:15", nhacNganSach = false, nhacMucTieu = false })).EnsureSuccessStatusCode();

        var r = await client.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay = true, gioNhac = "20:00", nhacNganSach = true, nhacMucTieu = true });

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await client.GetFromJsonAsync<JsonElement>("/api/thong-bao/cai-dat");
        json.GetProperty("nhacHangNgay").GetBoolean().Should().BeTrue();
        json.GetProperty("gioNhac").GetString().Should().Be("20:00");
        json.GetProperty("nhacNganSach").GetBoolean().Should().BeTrue();
        json.GetProperty("nhacMucTieu").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task KhoiPhucMacDinh_Post_DatLaiGiaTriMacDinh()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        (await client.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay = false, gioNhac = "06:15", nhacNganSach = false, nhacMucTieu = false })).EnsureSuccessStatusCode();

        var r = await client.PostAsync("/api/thong-bao/cai-dat/khoi-phuc", null);

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("nhacHangNgay").GetBoolean().Should().BeTrue();
        json.GetProperty("gioNhac").GetString().Should().Be("20:00");
        json.GetProperty("nhacNganSach").GetBoolean().Should().BeTrue();
        json.GetProperty("nhacMucTieu").GetBoolean().Should().BeTrue();
        (await client.GetFromJsonAsync<JsonElement>("/api/thong-bao/cai-dat")).GetProperty("gioNhac").GetString().Should().Be("20:00");
    }

    [Fact]
    public async Task CaiDat_CuaMoiNguoiRieng()
    {
        var a = await factory.TaoClientDaDangNhap(EmailMoi());
        var b = await factory.TaoClientDaDangNhap(EmailMoi());
        (await a.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay = false, gioNhac = "05:00", nhacNganSach = true, nhacMucTieu = true })).EnsureSuccessStatusCode();

        (await b.GetFromJsonAsync<JsonElement>("/api/thong-bao/cai-dat")).GetProperty("gioNhac").GetString().Should().Be("20:00");
    }

    [Fact]
    public async Task CaiDat_QuanTriVienKhongCo_404()
    {
        var admin = await factory.TaoClientAdmin();

        (await admin.GetAsync("/api/thong-bao/cai-dat")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PutAsJsonAsync("/api/thong-bao/cai-dat",
            new { nhacHangNgay = true, gioNhac = "20:00", nhacNganSach = true, nhacMucTieu = true }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PostAsync("/api/thong-bao/cai-dat/khoi-phuc", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
