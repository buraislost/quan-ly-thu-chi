namespace QuanLyThuChi.Jobs;

/// <summary>Nền chung của các bộ hẹn giờ: mỗi phút gọi <see cref="ChayMotLanAsync"/>; lỗi một vòng được ghi log và vòng sau vẫn chạy.</summary>
public abstract class BoHenGioMotPhut(ILogger logger, string tenJob) : BackgroundService
{
    /// <summary>Một vòng xử lý; lỗi được ném ra cho người gọi (vòng lặp nền ghi log rồi chạy tiếp).</summary>
    public abstract Task ChayMotLanAsync(CancellationToken ct);

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
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Vòng {TenJob} bị lỗi, sẽ thử lại ở vòng sau", tenJob);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Dừng ứng dụng.
        }
    }
}
