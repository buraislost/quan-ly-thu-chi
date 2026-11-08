using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDongHo, DongHoVietNam>();
builder.Services.AddSingleton<IBamMatKhau, BCryptBamMatKhau>();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("MacDinh")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    // Môi trường Testing do ApiFactory tự tạo schema (SQLite) và nạp dữ liệu khởi tạo.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DuLieuKhoiTao.NapAsync(db, scope.ServiceProvider.GetRequiredService<IBamMatKhau>(),
        scope.ServiceProvider.GetRequiredService<IDongHo>());
}

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

// Cho phép WebApplicationFactory<Program> trong project test.
public partial class Program { }
