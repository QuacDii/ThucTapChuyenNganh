using Microsoft.EntityFrameworkCore;
using TTCN.Models;

public class CleanupExpiredOrdersService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupExpiredOrdersService> _logger;

    public CleanupExpiredOrdersService(
        IServiceScopeFactory scopeFactory,
        ILogger<CleanupExpiredOrdersService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<QLDVContext>();

                var expiredOrders = await db.DonDatVes
                    .Where(d =>
                        (d.TrangThai == "Hết hạn"
                        || d.TrangThai == "Đã hủy"
                        || d.TrangThai == "Thanh toán lỗi")
                        && d.NgayDat < DateTime.Now.AddDays(-7))
                    .ToListAsync();

                if (expiredOrders.Any())
                {
                    db.DonDatVes.RemoveRange(expiredOrders);
                    await db.SaveChangesAsync();

                    _logger.LogInformation(
                        $" Cleanup: Đã xóa {expiredOrders.Count} đơn cũ");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi CleanupExpiredOrdersService");
            }


            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
