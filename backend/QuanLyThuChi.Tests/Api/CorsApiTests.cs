using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace QuanLyThuChi.Tests.Api;

public class CorsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static HttpRequestMessage Preflight(string nguon) =>
        new(HttpMethod.Options, "/api/auth/dang-nhap")
        {
            Headers =
            {
                { "Origin", nguon },
                { "Access-Control-Request-Method", "POST" },
                { "Access-Control-Request-Headers", "content-type,authorization" },
            },
        };

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("http://127.0.0.1:61234")]
    public async Task Preflight_TuLocalhost_DuocPhep(string nguon)
    {
        var r = await factory.CreateClient().SendAsync(Preflight(nguon));

        r.Headers.TryGetValues("Access-Control-Allow-Origin", out var giaTri).Should().BeTrue();
        giaTri.Should().ContainSingle().Which.Should().Be(nguon);
    }

    [Fact]
    public async Task Preflight_TuNguonLa_KhongDuocPhep()
    {
        var r = await factory.CreateClient().SendAsync(Preflight("https://trang-la.example.com"));

        r.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task CoCauHinhNguonChoPhep_ChiNguonDoDuocPhep()
    {
        using var f = factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:NguonChoPhep:0"] = "https://quanlythuchi.vn" })));
        var client = f.CreateClient();

        var hopLe = await client.SendAsync(Preflight("https://quanlythuchi.vn"));
        var localhost = await client.SendAsync(Preflight("http://localhost:5000"));

        hopLe.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be("https://quanlythuchi.vn");
        localhost.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
