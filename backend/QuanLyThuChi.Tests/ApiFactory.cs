using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Tests.Fakes;

namespace QuanLyThuChi.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    // Một connection SQLite in-memory được giữ mở suốt vòng đời factory để dữ liệu không mất.
    private readonly SqliteConnection _ketNoi = new("DataSource=:memory:");

    public DongHoGia DongHo { get; } = new();

    public ApiFactory()
    {
        _ketNoi.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Khoa"] = "khoa-chi-dung-cho-test-khong-dung-that-0123456789",
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDongHo>();
            services.AddSingleton<IDongHo>(DongHo);

            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_ketNoi));

            // Nạp controller của project test (route /api/_test/dto) để kiểm tra lỗi ModelState.
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);

            // Hook cho các task sau:
            // - Task 13: thay IDichVuOCR bằng FakeDichVuOCR.
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        DuLieuKhoiTao.NapAsync(db, scope.ServiceProvider.GetRequiredService<IBamMatKhau>(), DongHo).GetAwaiter().GetResult();
        return host;
    }

    /// <summary>Tạo scope mới có AppDbContext để test truy cập CSDL trực tiếp.</summary>
    public IServiceScope TaoScope()
    {
        _ = Server; // đảm bảo host đã khởi tạo
        return Services.CreateScope();
    }

    public T VoiDb<T>(Func<AppDbContext, T> hanhDong)
    {
        using var scope = TaoScope();
        return hanhDong(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Đăng ký (nếu email chưa có) rồi đăng nhập qua API; trả client đã gắn Bearer token.</summary>
    public async Task<HttpClient> TaoClientDaDangNhap(string email, string matKhau = "matkhau123")
    {
        var client = CreateClient();
        var daCo = VoiDb(db => db.TaiKhoan.Any(t => t.Email == email.Trim().ToLower()));
        if (!daCo)
        {
            var dangKy = await client.PostAsJsonAsync("/api/auth/dang-ky", new { hoTen = "Người dùng thử", email, matKhau });
            dangKy.EnsureSuccessStatusCode();
        }
        return await GanToken(client, email, matKhau);
    }

    /// <summary>Đăng nhập tài khoản quản trị viên khởi tạo (admin@demo.vn).</summary>
    public Task<HttpClient> TaoClientAdmin() =>
        GanToken(CreateClient(), DuLieuKhoiTao.EmailAdmin, DuLieuKhoiTao.MatKhauAdmin);

    private static async Task<HttpClient> GanToken(HttpClient client, string email, string matKhau)
    {
        var dangNhap = await client.PostAsJsonAsync("/api/auth/dang-nhap", new { email, matKhau });
        dangNhap.EnsureSuccessStatusCode();
        var token = (await dangNhap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _ketNoi.Dispose();
    }
}
