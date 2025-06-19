namespace Web0524.Models.Marketing
{
    public class CouponDispatchBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CouponDispatchBackgroundService> _logger;

        public CouponDispatchBackgroundService(IServiceProvider serviceProvider, ILogger<CouponDispatchBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var marketing = scope.ServiceProvider.GetRequiredService<IMarketingService>();
                        await marketing.RunAutoDispatchAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "執行自動派發時發生錯誤");
                }

                // 每天晚上 10 點執行一次
                var now = DateTime.Now;
                var nextRun = DateTime.Today.AddHours(22).AddMinutes(3); // 22:00
                if (now > nextRun)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);
            }
        }

    }


}
