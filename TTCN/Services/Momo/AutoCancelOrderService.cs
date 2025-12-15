using Microsoft.EntityFrameworkCore;
using TTCN.Models;

namespace TTCN.Services
{
    public class AutoCancelOrderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Thời gian giữ ghế (phút)
        private const int HOLD_MINUTES = 5;

        public AutoCancelOrderService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<QLDVContext>();

                var now = DateTime.Now;

                var expiredOrders = await db.DonDatVes
                    .Where(d =>
                        d.TrangThai == "Chờ thanh toán" &&
                        d.NgayDat.AddMinutes(HOLD_MINUTES) < now
                    )
                    .ToListAsync(stoppingToken);

                if (expiredOrders.Any())
                {
                    foreach (var don in expiredOrders)
                    {
                        // Cập nhật trạng thái
                        don.TrangThai = "Hết hạn";

                        // Xóa ghế đã giữ
                        var chiTiet = db.ChiTietDonDat
                            .Where(x => x.MaDon == don.MaDon);
                        db.ChiTietDonDat.RemoveRange(chiTiet);

                        // Xóa combo đã giữ
                        var combos = db.DonDatVeDoAns
                            .Where(x => x.MaDon == don.MaDon);
                        db.DonDatVeDoAns.RemoveRange(combos);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }

                // Kiểm tra mỗi 1 phút
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
