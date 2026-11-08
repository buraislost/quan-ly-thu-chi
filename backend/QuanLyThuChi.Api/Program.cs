using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    // Enum đi qua API dưới dạng tên ("Thu", "TienMat") để giao diện dễ đọc.
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .ToDictionary(
                e => string.Join('.', e.Key.Split('.').Select(p => p.Length == 0 ? p : char.ToLowerInvariant(p[0]) + p[1..])),
                e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Giá trị không hợp lệ" : x.ErrorMessage).ToArray());
        return new BadRequestObjectResult(new { message = "Dữ liệu không hợp lệ", errors });
    });

// Cấu hình JWT đọc lười (qua options) để ApiFactory ghi đè được trong test; thiếu/ngắn Khoa thì app không khởi động.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.TenMuc))
    .Validate(o => o.Khoa?.Length >= JwtOptions.DoDaiKhoaToiThieu,
        $"Jwt:Khoa phải có ít nhất {JwtOptions.DoDaiKhoaToiThieu} ký tự (xem README, mục cấu hình)")
    .ValidateOnStart();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((o, jwt) =>
    {
        var cfg = jwt.Value;
        o.MapInboundClaims = false; // giữ nguyên tên claim "sub" và "role"
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg.PhatHanh,
            ValidateAudience = true,
            ValidAudience = cfg.DoiTuong,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtService.TaoKhoaKy(cfg.Khoa),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtService.ClaimSub,
            RoleClaimType = JwtService.ClaimRole, // để [Authorize(Roles=...)] và User.IsInRole hoạt động
        };
        o.Events = new JwtBearerEvents
        {
            // R8: tài khoản bị khóa hoặc đã xóa thì token còn hạn cũng bị từ chối.
            OnTokenValidated = async ctx =>
            {
                if (ctx.Principal is null || !int.TryParse(ctx.Principal.FindFirst(JwtService.ClaimSub)?.Value, out var ma))
                {
                    ctx.Fail("Token không hợp lệ");
                    return;
                }
                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var trangThai = await db.TaiKhoan.AsNoTracking()
                    .Where(t => t.MaTaiKhoan == ma)
                    .Select(t => (TrangThaiTaiKhoan?)t.TrangThai)
                    .FirstOrDefaultAsync(ctx.HttpContext.RequestAborted);
                if (trangThai is null || trangThai == TrangThaiTaiKhoan.BiKhoa)
                    ctx.Fail("Tài khoản không tồn tại hoặc đã bị khóa");
            },
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDongHo, DongHoVietNam>();
builder.Services.AddSingleton<IBamMatKhau, BCryptBamMatKhau>();
builder.Services.AddScoped<QuanLyThuChi.Services.TaiKhoanService>();
builder.Services.AddScoped<QuanLyThuChi.Services.ViService>();
builder.Services.AddScoped<QuanLyThuChi.Services.DanhMucService>();
builder.Services.AddScoped<QuanLyThuChi.Services.ThongBaoService>();
builder.Services.AddScoped<QuanLyThuChi.Services.NganSachService>();
builder.Services.AddScoped<QuanLyThuChi.Services.GiaoDichService>();
builder.Services.AddScoped<QuanLyThuChi.Services.MucTieuService>();
builder.Services.AddScoped<QuanLyThuChi.Services.BaoCaoService>();
builder.Services.AddScoped<QuanLyThuChi.Services.QuanTriService>();
// Bộ hẹn giờ: luôn đăng ký singleton để test gọi trực tiếp; chỉ chạy nền ngoài môi trường Testing.
builder.Services.AddSingleton<QuanLyThuChi.Jobs.NhacNhoHangNgayJob>();
builder.Services.AddSingleton<QuanLyThuChi.Jobs.TongKetThangJob>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService(sp => sp.GetRequiredService<QuanLyThuChi.Jobs.NhacNhoHangNgayJob>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<QuanLyThuChi.Jobs.TongKetThangJob>());
}
builder.Services.AddOptions<GeminiOptions>().Bind(builder.Configuration.GetSection(GeminiOptions.TenMuc));
builder.Services.AddOptions<LuuTepOptions>().Bind(builder.Configuration.GetSection(LuuTepOptions.TenMuc));
builder.Services.AddScoped<LuuTepService>();
builder.Services.AddScoped<QuanLyThuChi.Services.HoaDonService>();
if (builder.Configuration.GetValue<bool>($"{GeminiOptions.TenMuc}:{nameof(GeminiOptions.DungBanGia)}"))
{
    // Demo offline: hóa đơn mẫu cố định, không gọi Gemini.
    builder.Services.AddSingleton<IDichVuOCR, DichVuOCRMau>();
}
else
{
    // Thiếu ApiKey không làm hỏng khởi động; lỗi chỉ phát sinh khi quét (GeminiDichVuOCR).
    builder.Services.AddHttpClient<IDichVuOCR, GeminiDichVuOCR>(c => c.Timeout = TimeSpan.FromSeconds(30));
}
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
    if (app.Environment.IsDevelopment())
    {
        // Dữ liệu demo chỉ nạp khi phát triển (idempotent theo email demo@demo.vn).
        await DuLieuDemo.NapAsync(db, scope.ServiceProvider.GetRequiredService<IBamMatKhau>(),
            scope.ServiceProvider.GetRequiredService<IDongHo>());
    }
}

app.UseMiddleware<XuLyLoiMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

// Phục vụ ảnh hóa đơn đã tải lên tại /uploads/... (cấu hình đọc sau Build để test ghi đè được LuuTep:ThuMuc).
var thuMucTep = LuuTepService.ThuMucGoc(app.Environment.ContentRootPath,
    app.Configuration.GetSection(LuuTepOptions.TenMuc).Get<LuuTepOptions>() ?? new LuuTepOptions());
Directory.CreateDirectory(Path.Combine(thuMucTep, LuuTepService.ThuMucCon));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(thuMucTep),
    RequestPath = LuuTepService.TienToUrl,
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsEnvironment("Testing"))
{
    // Route thử chỉ tồn tại trong môi trường Testing.
    var thu = app.MapGroup("/api/_test");
    thu.MapGet("/loi-nghiep-vu", () => { throw new DomainException("Lỗi thử"); });
    thu.MapGet("/khong-tim-thay", () => { throw new KhongTimThayException(); });
    thu.MapGet("/loi-khac", () => { throw new InvalidOperationException("bí mật nội bộ"); });
    thu.MapGet("/can-dang-nhap", (System.Security.Claims.ClaimsPrincipal user) => new { maTaiKhoan = user.MaTaiKhoan() })
        .RequireAuthorization();
    thu.MapGet("/chi-admin", [Authorize(Roles = "QuanTriVien")] () => Results.Ok());
}

app.Run();

// Cho phép WebApplicationFactory<Program> trong project test.
public partial class Program { }
