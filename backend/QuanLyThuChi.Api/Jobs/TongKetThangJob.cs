using QuanLyThuChi.Domain;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Jobs;

/// <summary>
/// Bộ hẹn giờ tổng kết tháng: mỗi phút kiểm tra, chỉ chạy vào ngày cuối tháng từ 23:00 đến 23:59 (giờ Việt Nam)
/// và mỗi tháng một lần. Khởi động lại giữa chừng vẫn an toàn vì BaoCaoService đảm bảo mỗi tháng một báo cáo (R9).
/// </summary>
public class TongKetThangJob(IServiceScopeFactory scopeFactory, IDongHo dongHo, ILogger<TongKetThangJob> logger)
    : BackgroundService
{
    private (int Thang, int Nam)? _daChay;

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
                    logger.LogError(ex, "Vòng tổng kết tháng bị lỗi, sẽ thử lại ở vòng sau");
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
        var laNgayCuoiThang = bayGio.Day == DateTime.DaysInMonth(bayGio.Year, bayGio.Month);
        if (!laNgayCuoiThang || bayGio.Hour != 23) return;

        var ky = (bayGio.Month, bayGio.Year);
        if (_daChay == ky) return;

        using var scope = scopeFactory.CreateScope();
        var baoCao = scope.ServiceProvider.GetRequiredService<BaoCaoService>();
        await baoCao.TongKetTatCaAsync(ky.Month, ky.Year, ct);
        _daChay = ky;
    }
}
