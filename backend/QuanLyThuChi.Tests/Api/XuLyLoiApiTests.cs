using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Tests.Api;

public class XuLyLoiApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private JwtService Jwt => factory.Services.GetRequiredService<JwtService>();

    private TaiKhoan Admin() =>
        factory.VoiDb(db => db.TaiKhoan.AsNoTracking().Single(t => t.Email == DuLieuKhoiTao.EmailAdmin));

    private NguoiDung ThemNguoiDung()
    {
        var email = $"nd{Guid.NewGuid():N}@demo.vn";
        return factory.VoiDb(db =>
        {
            var nd = new NguoiDung("Người dùng thử", email, "hash", DateTime.UtcNow);
            db.TaiKhoan.Add(nd);
            db.SaveChanges();
            return nd;
        });
    }

    private HttpClient ClientVoiToken(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task DomainException_Tra400CoMessage()
    {
        var response = await factory.CreateClient().GetAsync("/api/_test/loi-nghiep-vu");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("message").GetString().Should().Be("Lỗi thử");
    }

    [Fact]
    public async Task KhongTimThay_Tra404()
    {
        var response = await factory.CreateClient().GetAsync("/api/_test/khong-tim-thay");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("message").GetString().Should().Be("Không tìm thấy dữ liệu");
    }

    [Fact]
    public async Task LoiKhac_Tra500KhongLoChiTiet()
    {
        var response = await factory.CreateClient().GetAsync("/api/_test/loi-khac");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;
        json.GetProperty("message").GetString().Should().Be("Đã xảy ra lỗi, vui lòng thử lại");
        body.Should().NotContain("bí mật nội bộ").And.NotContain("InvalidOperationException");
    }

    [Fact]
    public async Task KhongToken_Tra401()
    {
        var response = await factory.CreateClient().GetAsync("/api/_test/can-dang-nhap");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenHopLe_Tra200VaDocDuocMaTaiKhoan()
    {
        var nd = ThemNguoiDung();

        var response = await ClientVoiToken(Jwt.TaoToken(nd)).GetAsync("/api/_test/can-dang-nhap");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("maTaiKhoan").GetInt32().Should().Be(nd.MaTaiKhoan);
    }

    [Fact]
    public async Task TokenSaiChuKy_Tra401()
    {
        var nd = ThemNguoiDung();
        var khoaLa = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 40)));
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            "QuanLyThuChi", "QuanLyThuChi.Web",
            [new Claim("sub", nd.MaTaiKhoan.ToString()), new Claim("role", "NguoiDung")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(khoaLa, SecurityAlgorithms.HmacSha256)));

        var response = await ClientVoiToken(token).GetAsync("/api/_test/can-dang-nhap");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChiAdmin_NguoiDungBi403_QuanTriVienDuoc200()
    {
        var nd = ThemNguoiDung();

        var voiNguoiDung = await ClientVoiToken(Jwt.TaoToken(nd)).GetAsync("/api/_test/chi-admin");
        var voiAdmin = await ClientVoiToken(Jwt.TaoToken(Admin())).GetAsync("/api/_test/chi-admin");

        voiNguoiDung.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        voiAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TaiKhoanBiKhoa_TokenCu_Tra401()
    {
        var nd = ThemNguoiDung();
        var token = Jwt.TaoToken(nd);
        factory.VoiDb(db =>
        {
            var admin = (QuanTriVien)db.TaiKhoan.Single(t => t.Email == DuLieuKhoiTao.EmailAdmin);
            var canKhoa = db.TaiKhoan.Single(t => t.MaTaiKhoan == nd.MaTaiKhoan);
            admin.KhoaTaiKhoan(canKhoa, "Vi phạm");
            return db.SaveChanges();
        });

        var response = await ClientVoiToken(token).GetAsync("/api/_test/can-dang-nhap");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TaiKhoanKhongConTonTai_Tra401()
    {
        var nd = ThemNguoiDung();
        var token = Jwt.TaoToken(nd);
        factory.VoiDb(db =>
        {
            db.TaiKhoan.Remove(db.TaiKhoan.Single(t => t.MaTaiKhoan == nd.MaTaiKhoan));
            return db.SaveChanges();
        });

        var response = await ClientVoiToken(token).GetAsync("/api/_test/can-dang-nhap");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ModelStateSai_Tra400KemDanhSachLoi()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/_test/dto", new { soLuong = 500 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("message").GetString().Should().Be("Dữ liệu không hợp lệ");
        var errors = json.GetProperty("errors");
        errors.GetProperty("tenGoi")[0].GetString().Should().Be("Vui lòng nhập tên");
        errors.GetProperty("soLuong")[0].GetString().Should().Be("Số lượng phải từ 1 đến 100");
    }

    [Fact]
    public void TaoToken_CoSubVaRole_HanTamGio()
    {
        var token = Jwt.TaoToken(Admin());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Subject.Should().Be(Admin().MaTaiKhoan.ToString());
        jwt.Claims.Single(c => c.Type == "role").Value.Should().Be("QuanTriVien");
        (jwt.ValidTo - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromHours(8), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void MaTaiKhoan_ThieuClaimSub_NemLoi()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", "NguoiDung")], "test"));

        var act = () => principal.MaTaiKhoan();

        act.Should().Throw<InvalidOperationException>();
    }
}
