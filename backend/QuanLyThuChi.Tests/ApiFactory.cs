using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Tests.Fakes;

namespace QuanLyThuChi.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public DongHoGia DongHo { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDongHo>();
            services.AddSingleton<IDongHo>(DongHo);

            // Hook cho các task sau:
            // - Task 5: thay SQL Server bằng SQLite in-memory (giữ connection mở).
            // - Task 13: thay IDichVuOCR bằng FakeDichVuOCR.
        });
    }
}
