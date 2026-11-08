using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Jobs;

/// <summary>
/// Bộ hẹn giờ nhắc nhở: mỗi phút xét những người dùng có giờ nhắc trùng giờ:phút hiện tại (giờ Việt Nam),
/// nhắc ghi chép nếu hôm nay chưa có giao dịch và nhắc các mục tiêu tiết kiệm đang chậm tiến độ.
/// </summary>
public class NhacNhoHangNgayJob(IServiceScopeFactory scopeFactory, IDongHo dongHo, ILogger<NhacNhoHangNgayJob> logger)
    : BackgroundService
{
    public const string NoiDungNhacGhiChep = "Hôm nay bạn chưa ghi chép chi tiêu nào. Dành 1 phút ghi lại nhé!";
    private static readonly CultureInfo ViVN = CultureInfo.GetCultureInfo("vi-VN");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            do
            {
                try
                {
                    await ChayMotLanAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Vòng nhắc nhở hằng ngày bị lỗi, sẽ thử lại ở vòng sau");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Dừng ứng dụng.
        }
    }

    /// <summary>Một vòng xử lý; lỗi được ném ra cho người gọi (vòng lặp nền ghi log rồi chạy tiếp).</summary>
    public async Task ChayMotLanAsync(CancellationToken ct = default)
    {
        var bayGio = dongHo.BayGio;
        var homNay = dongHo.HomNay;
        var gioPhut = new TimeOnly(bayGio.Hour, bayGio.Minute);
        var dauNgay = homNay.ToDateTime(TimeOnly.MinValue);
        var cuoiNgay = dauNgay.AddDays(1);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var thongBao = scope.ServiceProvider.GetRequiredService<ThongBaoService>();

        var dsNguoiDung = await db.Set<NguoiDung>().Include(u => u.CaiDat)
            .Where(u => u.TrangThai == TrangThaiTaiKhoan.HoatDong && u.CaiDat.GioNhac == gioPhut)
            .ToListAsync(ct);

        foreach (var u in dsNguoiDung)
        {
            if (u.CaiDat.NhacHangNgay
                && !await db.GiaoDich.AnyAsync(g => g.MaTaiKhoan == u.MaTaiKhoan && g.Ngay >= dauNgay && g.Ngay < cuoiNgay, ct)
                && !await thongBao.DaGuiHomNayAsync(u.MaTaiKhoan, LoaiThongBao.NhacGhiChep, ct))
            {
                thongBao.Tao(u.MaTaiKhoan, LoaiThongBao.NhacGhiChep, NoiDungNhacGhiChep);
            }

            if (u.CaiDat.NhacMucTieu)
            {
                var mucTieu = (await db.MucTieuTietKiem.AsNoTracking().Where(m => m.MaTaiKhoan == u.MaTaiKhoan).ToListAsync(ct))
                    .OrderBy(m => m.MaMucTieu)
                    .FirstOrDefault(m => m.LaChamTienDo(homNay));
                if (mucTieu is not null && !await thongBao.DaGuiHomNayAsync(u.MaTaiKhoan, LoaiThongBao.NhacMucTieu, ct))
                {
                    thongBao.Tao(u.MaTaiKhoan, LoaiThongBao.NhacMucTieu, string.Create(ViVN,
                        $"Mục tiêu \"{mucTieu.TenMucTieu}\" đang chậm tiến độ, nên nạp thêm {mucTieu.SoTienCanMoiThang(homNay):N0}đ mỗi tháng"));
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
