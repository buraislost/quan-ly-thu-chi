using Microsoft.AspNetCore.Hosting;
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
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDongHo>();
            services.AddSingleton<IDongHo>(DongHo);

            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_ketNoi));

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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _ketNoi.Dispose();
    }
}
