using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Tests.Fakes;

namespace QuanLyThuChi.Tests.Api;

public class HoaDonApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly byte[] ChuKyPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] ChuKyJpeg = [0xFF, 0xD8, 0xFF, 0xE0];

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static byte[] TaoAnh(byte[] chuKy, int dungLuong)
    {
        var b = new byte[dungLuong];
        chuKy.CopyTo(b, 0);
        return b;
    }

    private static Task<HttpResponseMessage> Quet(HttpClient c, byte[] noiDung, string contentType, string tenTep)
    {
        var tep = new ByteArrayContent(noiDung);
        tep.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent { { tep, "anh", tenTep } };
        return c.PostAsync("/api/hoa-don/quet", form);
    }

    private int SoTepDaLuu()
    {
        var thuMuc = Path.Combine(factory.ThuMucTep, "hoa-don");
        return Directory.Exists(thuMuc) ? Directory.GetFiles(thuMuc).Length : 0;
    }

    private async Task<HttpClient> Client() =>
        await factory.TaoClientDaDangNhap($"u{Guid.NewGuid():N}@demo.vn");

    [Fact]
    public async Task Quet_AnhPng_TraBanNhap()
    {
        factory.Ocr.NemLoi = false;
        factory.Ocr.KetQua = FakeDichVuOCR.KetQuaMacDinh();
        var c = await Client();

        var r = await Quet(c, TaoAnh(ChuKyPng, 2048), "image/png", "bill.png");

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        j.GetProperty("tenCuaHang").GetString().Should().Be("Bách Hóa Xanh");
        j.GetProperty("ngay").GetString().Should().Be("2026-11-07");
        j.GetProperty("tongTien").GetDecimal().Should().Be(75_000m);
        j.GetProperty("matHang").GetArrayLength().Should().Be(2);
        var url = j.GetProperty("anhUrl").GetString()!;
        url.Should().StartWith("/uploads/hoa-don/").And.EndWith(".png");
        factory.Ocr.MimeCuoi.Should().Be("image/png");

        var anh = await c.GetAsync(url);
        anh.StatusCode.Should().Be(HttpStatusCode.OK);
        (await anh.Content.ReadAsByteArrayAsync()).Should().HaveCount(2048);
    }

    [Fact]
    public async Task Quet_AnhJpeg_TraBanNhap()
    {
        factory.Ocr.NemLoi = false;
        var r = await Quet(await Client(), TaoAnh(ChuKyJpeg, 1024), "image/jpeg", "bill.jpg");

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("anhUrl").GetString().Should().EndWith(".jpg");
    }

    [Fact]
    public async Task Quet_FileTxt_400()
    {
        var truoc = factory.Ocr.SoLanGoi;
        var tepTruoc = SoTepDaLuu();
        var r = await Quet(await Client(), "xin chào"u8.ToArray(), "text/plain", "bill.txt");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Chỉ nhận ảnh JPG hoặc PNG");
        factory.Ocr.SoLanGoi.Should().Be(truoc);
        SoTepDaLuu().Should().Be(tepTruoc);
    }

    [Fact]
    public async Task Quet_ContentTypeAnhNhungNoiDungKhongPhaiAnh_400()
    {
        var r = await Quet(await Client(), "xin chào"u8.ToArray(), "image/png", "gia.png");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Chỉ nhận ảnh JPG hoặc PNG");
    }

    [Fact]
    public async Task Quet_Anh6MB_400()
    {
        var tepTruoc = SoTepDaLuu();
        var r = await Quet(await Client(), TaoAnh(ChuKyPng, 6 * 1024 * 1024), "image/png", "to.png");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Ảnh vượt quá 5 MB");
        SoTepDaLuu().Should().Be(tepTruoc);
    }

    [Fact]
    public async Task Quet_OCRNemLoi_400KhongNhanDangDuoc()
    {
        factory.Ocr.NemLoi = true;
        var c = await Client();
        var truoc = SoTepDaLuu();
        try
        {
            var r = await Quet(c, TaoAnh(ChuKyPng, 512), "image/png", "bill.png");

            r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var j = await r.Content.ReadFromJsonAsync<JsonElement>();
            j.GetProperty("message").GetString().Should().Be("Không nhận dạng được hóa đơn");
            var url = j.GetProperty("anhUrl").GetString()!;
            url.Should().StartWith("/uploads/hoa-don/");
            SoTepDaLuu().Should().Be(truoc + 1);
            File.Exists(Path.Combine(factory.ThuMucTep, "hoa-don", Path.GetFileName(url))).Should().BeTrue();
            (await c.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally { factory.Ocr.NemLoi = false; }
    }

    [Fact]
    public async Task Quet_ThieuTruongAnh_400()
    {
        var r = await (await Client()).PostAsync("/api/hoa-don/quet", new MultipartFormDataContent { { new StringContent("x"), "khac" } });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Quet_ChuaDangNhap_401()
    {
        var r = await Quet(factory.CreateClient(), TaoAnh(ChuKyPng, 512), "image/png", "bill.png");

        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
