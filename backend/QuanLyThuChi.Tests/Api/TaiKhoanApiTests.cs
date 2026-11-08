using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Api;

public class TaiKhoanApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    [Fact]
    public async Task DangKy_TaoViTienMatVaCaiDat()
    {
        var email = EmailMoi();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/dang-ky",
            new { hoTen = "Nguyễn Văn A", email = $"  {email.ToUpperInvariant()} ", matKhau = "matkhau123" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("vaiTro").GetString().Should().Be("NguoiDung");
        json.GetProperty("hoTen").GetString().Should().Be("Nguyễn Văn A");

        factory.VoiDb(db =>
        {
            var nd = db.TaiKhoan.OfType<NguoiDung>().Include(x => x.CaiDat).Single(t => t.Email == email);
            nd.CaiDat.NhacHangNgay.Should().BeTrue();
            nd.CaiDat.GioNhac.Should().Be(CaiDatNhacNho.GioNhacMacDinh);
            var vi = db.Vi.Where(v => v.MaTaiKhoan == nd.MaTaiKhoan).ToList();
            vi.Should().ContainSingle();
            vi[0].TenVi.Should().Be("Tiền mặt");
            vi[0].LoaiVi.Should().Be(LoaiVi.TienMat);
            vi[0].SoDu.Should().Be(0);
            return 0;
        });
    }

    [Fact]
    public async Task DangKy_EmailTrung_400()
    {
        var email = EmailMoi();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/dang-ky", new { hoTen = "A", email, matKhau = "matkhau123" });

        var response = await client.PostAsJsonAsync("/api/auth/dang-ky",
            new { hoTen = "B", email = email.ToUpperInvariant(), matKhau = "matkhau123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(response)).Should().Be("Email đã được sử dụng");
    }

    [Fact]
    public async Task DangKy_DuLieuSai_400KemLoi()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/dang-ky",
            new { hoTen = "A", email = "khong-phai-email", matKhau = "123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("email")[0].GetString().Should().Be("Email không hợp lệ");
        errors.GetProperty("matKhau")[0].GetString().Should().Be("Mật khẩu tối thiểu 6 ký tự");
    }

    [Fact]
    public async Task DangNhap_SaiMatKhau_400()
    {
        var email = EmailMoi();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/dang-ky", new { hoTen = "A", email, matKhau = "matkhau123" });

        var sai = await client.PostAsJsonAsync("/api/auth/dang-nhap", new { email, matKhau = "saimatkhau" });
        var khongTonTai = await client.PostAsJsonAsync("/api/auth/dang-nhap", new { email = EmailMoi(), matKhau = "matkhau123" });

        sai.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(sai)).Should().Be("Email hoặc mật khẩu không đúng");
        khongTonTai.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(khongTonTai)).Should().Be("Email hoặc mật khẩu không đúng");
    }

    [Fact]
    public async Task DangNhap_Dung_TraToken()
    {
        var email = EmailMoi();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/dang-ky", new { hoTen = "Trần B", email, matKhau = "matkhau123" });

        var response = await client.PostAsJsonAsync("/api/auth/dang-nhap",
            new { email = email.ToUpperInvariant(), matKhau = "matkhau123" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("vaiTro").GetString().Should().Be("NguoiDung");
        json.GetProperty("hoTen").GetString().Should().Be("Trần B");
    }

    [Fact]
    public async Task DangNhap_Admin_VaiTroQuanTriVien()
    {
        var client = await factory.TaoClientAdmin();

        var response = await client.GetAsync("/api/tai-khoan/toi");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("vaiTro").GetString().Should().Be("QuanTriVien");
        json.GetProperty("email").GetString().Should().Be(DuLieuKhoiTao.EmailAdmin);
    }

    [Fact]
    public async Task DangNhap_TaiKhoanBiKhoa_400()
    {
        var email = EmailMoi();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/dang-ky", new { hoTen = "A", email, matKhau = "matkhau123" });
        KhoaTrucTiep(email);

        var response = await client.PostAsJsonAsync("/api/auth/dang-nhap", new { email, matKhau = "matkhau123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(response)).Should().Be("Tài khoản đã bị khóa");
    }

    [Fact]
    public async Task TaiKhoanBiKhoa_RequestTiepTheo_401()
    {
        var email = EmailMoi();
        var client = await factory.TaoClientDaDangNhap(email);
        (await client.GetAsync("/api/tai-khoan/toi")).StatusCode.Should().Be(HttpStatusCode.OK);

        KhoaTrucTiep(email);

        (await client.GetAsync("/api/tai-khoan/toi")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Toi_KhongToken_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/tai-khoan/toi");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task XemVaSuaHoSo()
    {
        var email = EmailMoi();
        var client = await factory.TaoClientDaDangNhap(email);

        var xem = await (await client.GetAsync("/api/tai-khoan/toi")).Content.ReadFromJsonAsync<JsonElement>();
        xem.GetProperty("email").GetString().Should().Be(email);
        xem.GetProperty("vaiTro").GetString().Should().Be("NguoiDung");
        xem.GetProperty("donViTienTe").GetString().Should().Be("VND");
        xem.GetProperty("maTaiKhoan").GetInt32().Should().BeGreaterThan(0);

        var sua = await client.PutAsJsonAsync("/api/tai-khoan/toi", new { hoTen = "  Tên Mới " });
        sua.StatusCode.Should().Be(HttpStatusCode.OK);
        (await sua.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("hoTen").GetString().Should().Be("Tên Mới");

        var lai = await (await client.GetAsync("/api/tai-khoan/toi")).Content.ReadFromJsonAsync<JsonElement>();
        lai.GetProperty("hoTen").GetString().Should().Be("Tên Mới");
    }

    [Fact]
    public async Task SuaHoSo_TenTrong_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var response = await client.PutAsJsonAsync("/api/tai-khoan/toi", new { hoTen = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DoiMatKhau_Dung_DangNhapBangMatKhauMoi()
    {
        var email = EmailMoi();
        var client = await factory.TaoClientDaDangNhap(email);

        var doi = await client.PutAsJsonAsync("/api/tai-khoan/mat-khau",
            new { matKhauCu = "matkhau123", matKhauMoi = "matkhaumoi456" });

        doi.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var anon = factory.CreateClient();
        (await anon.PostAsJsonAsync("/api/auth/dang-nhap", new { email, matKhau = "matkhaumoi456" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await anon.PostAsJsonAsync("/api/auth/dang-nhap", new { email, matKhau = "matkhau123" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DoiMatKhau_SaiMatKhauCu_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var response = await client.PutAsJsonAsync("/api/tai-khoan/mat-khau",
            new { matKhauCu = "khongdung", matKhauMoi = "matkhaumoi456" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(response)).Should().Be("Mật khẩu hiện tại không đúng");
    }

    [Fact]
    public async Task DoiMatKhau_MoiQuaNgan_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var response = await client.PutAsJsonAsync("/api/tai-khoan/mat-khau",
            new { matKhauCu = "matkhau123", matKhauMoi = "123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("matKhauMoi")[0].GetString().Should().Be("Mật khẩu tối thiểu 6 ký tự");
    }

    private void KhoaTrucTiep(string email) => factory.VoiDb(db =>
    {
        var admin = (QuanTriVien)db.TaiKhoan.Single(t => t.Email == DuLieuKhoiTao.EmailAdmin);
        admin.KhoaTaiKhoan(db.TaiKhoan.Single(t => t.Email == email), "Vi phạm");
        return db.SaveChanges();
    });
}
