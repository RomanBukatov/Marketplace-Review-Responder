using Microsoft.Extensions.Options;
using WbAutoresponder.Configuration;
using WbAutoresponder.Services;

namespace WbAutoresponder
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IWildberriesApiClient _wbApiClient;
        private readonly IOzonApiClient _ozonApiClient;
        private readonly WorkerSettings _workerSettings;

        public Worker(
            ILogger<Worker> logger,
            IWildberriesApiClient wbApiClient,
            IOzonApiClient ozonApiClient,
            IOptions<WorkerSettings> workerSettings)
        {
            _logger = logger;
            _wbApiClient = wbApiClient;
            _ozonApiClient = ozonApiClient;
            _workerSettings = workerSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker запущен в: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _wbApiClient.CheckForNewReviewsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошла ошибка при проверке отзывов");
                }

                // Добавляем вызов Ozon
                try
                {
                    await _ozonApiClient.CheckForNewReviewsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка Ozon");
                }
                _logger.LogInformation("\n==================== ЦИКЛ ОБРАБОТКИ ЗАВЕРШЕН ====================\n");
                
                var delay = TimeSpan.FromSeconds(_workerSettings.CheckIntervalSeconds);
                _logger.LogInformation("Следующая проверка через {Delay} секунд...", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
