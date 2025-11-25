using Microsoft.Extensions.Options;
using WbAutoresponder.Configuration;

namespace WbAutoresponder.Services
{
    public class OzonApiClient : IOzonApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OzonApiClient> _logger;
        private readonly ApiKeys _apiKeys;
        private readonly IOpenAiClient _openAiClient;

        public OzonApiClient(
            HttpClient httpClient,
            ILogger<OzonApiClient> logger,
            IOptions<ApiKeys> apiKeys,
            IOpenAiClient openAiClient)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKeys = apiKeys.Value;
            _openAiClient = openAiClient;

            // Базовый адрес Ozon Seller API
            _httpClient.BaseAddress = new Uri("https://api-seller.ozon.ru");
            
            // Ozon требует два заголовка для авторизации
            _httpClient.DefaultRequestHeaders.Add("Client-Id", _apiKeys.OzonClientId);
            _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKeys.OzonApiKey);
        }

        public async Task CheckForNewReviewsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Проверяем наличие новых отзывов на Ozon... (Пока заглушка)");
            await Task.CompletedTask;
        }
    }
}