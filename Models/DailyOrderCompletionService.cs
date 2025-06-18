using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Web0524.Models;

public class DailyOrderCompletionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1);

    public DailyOrderCompletionService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 等待至明天凌晨 01:00 開始執行
        var now = DateTime.Now;
        var nextRunTime = DateTime.Today.AddDays(1).AddHours(1); // 明天凌晨 1 點
        var initialDelay = nextRunTime - now;

        if (initialDelay.TotalMilliseconds > 0)
            await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

                try
                {
                    int count = reservationService.AutoCompleteExpiredOrders();
                    Console.WriteLine($"✅ 背景任務：自動完成 {count} 筆過期預約");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 背景任務錯誤：{ex.Message}");
                }
            }

            await Task.Delay(_interval, stoppingToken); // 每 24 小時執行一次
        }
    }
}
