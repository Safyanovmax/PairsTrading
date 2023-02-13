using AppCore.Interfaces;
using TradingWorker.Services;

namespace TradingWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                using (var scope = _serviceProvider.CreateScope())
                {
                    bool error = false;
                    var pairsTradingService = scope.ServiceProvider.GetService<PairsTradingService>();
                    var telegramNotificationService = scope.ServiceProvider.GetService<ITelegramNotificationService>();
                    try
                    {
                        await pairsTradingService.Process();
                    }
                    catch (Exception ex)
                    {
                        await telegramNotificationService.Send(ex.Message + ex.StackTrace);
                        error = true;
                    }

                    if (error)
                    {
                        break;
                    }
                }

                await Task.Delay(60000, stoppingToken);
            }
        }
    }
}