using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using WbAutoresponder.Configuration;
using WbAutoresponder.DTOs.Ozon;

namespace WbAutoresponder.Services
{
    public class OzonApiClient : IOzonApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OzonApiClient> _logger;
        private readonly IOpenAiClient _openAiClient;
        private readonly ApiKeys _apiKeys;

        public OzonApiClient(
            HttpClient httpClient,
            ILogger<OzonApiClient> logger,
            IOptions<ApiKeys> apiKeys,
            IOpenAiClient openAiClient)
        {
            _httpClient = httpClient;
            _logger = logger;
            _openAiClient = openAiClient;
            _apiKeys = apiKeys.Value;

            // ЗАЩИТА: Проверяем, что ключи Ozon на месте
            if (string.IsNullOrWhiteSpace(_apiKeys.OzonClientId) || string.IsNullOrWhiteSpace(_apiKeys.OzonApiKey))
            {
                // Логируем как Warning, чтобы не валить приложение, если клиент хочет использовать только WB
                _logger.LogWarning("КЛЮЧИ OZON НЕ НАЙДЕНЫ. Модуль Ozon будет пропускать работу. Проверьте appsettings.json.");
            }

            _httpClient.BaseAddress = new Uri("https://api-seller.ozon.ru");
            _httpClient.DefaultRequestHeaders.Add("Client-Id", _apiKeys.OzonClientId);
            _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKeys.OzonApiKey);
        }

        public async Task CheckForNewReviewsAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_apiKeys.OzonClientId) || string.IsNullOrWhiteSpace(_apiKeys.OzonApiKey))
            {
                return; // Просто молча выходим, если ключей нет
            }
            
            _logger.LogInformation("Проверяем наличие новых отзывов на Ozon...");

            try
            {
                // 1. Получаем список отзывов (В Ozon это POST запрос с фильтром)
                var request = new OzonReviewListRequest();
                var jsonContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/v1/review/list", jsonContent, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ошибка Ozon API: {StatusCode}", response.StatusCode);
                    return;
                }

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var ozonData = await JsonSerializer.DeserializeAsync<OzonReviewListResponse>(stream, cancellationToken: cancellationToken);

                if (ozonData?.Result.Reviews.Count == 0)
                {
                    _logger.LogInformation("Новых отзывов на Ozon нет.");
                    return;
                }

                _logger.LogInformation("Найдено {Count} новых отзывов на Ozon.", ozonData.Result.Reviews.Count);

                foreach (var review in ozonData.Result.Reviews)
                {
                    await ProcessReviewAsync(review, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке Ozon.");
            }
        }

        private async Task ProcessReviewAsync(OzonReview review, CancellationToken cancellationToken)
        {
            // Собираем полный текст
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(review.Text.Comment)) parts.Add(review.Text.Comment);
            if (!string.IsNullOrWhiteSpace(review.Text.Positive)) parts.Add($"Достоинства: {review.Text.Positive}");
            if (!string.IsNullOrWhiteSpace(review.Text.Negative)) parts.Add($"Недостатки: {review.Text.Negative}");

            var fullText = string.Join("\n", parts);
            var productName = review.Product.Title; // Можно добавить в промпт, если нужно

            // Если отзыв пустой
            if (string.IsNullOrWhiteSpace(fullText))
            {
                _logger.LogWarning("Отзыв Ozon ID: {Id} пустой. Отправляем заглушку.", review.Id);
                await SendAnswerAsync(review.Id, "Благодарим за высокую оценку!", cancellationToken);
                return;
            }

            // Генерируем ответ
            var aiResponse = await _openAiClient.GetResponseForFeedback(fullText, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                _logger.LogWarning("OpenAI не ответил на отзыв {Id}", review.Id);
                return;
            }

            _logger.LogInformation("Для отзыва Ozon '{Text}' сгенерирован ответ: '{Answer}'", fullText.Replace("\n", "|"), aiResponse);

            // Отправляем
            await SendAnswerAsync(review.Id, aiResponse, cancellationToken);
        }

        private async Task SendAnswerAsync(string reviewId, string text, CancellationToken cancellationToken)
        {
            try
            {
                var answerRequest = new OzonAnswerRequest(reviewId, text);
                var jsonContent = new StringContent(JsonSerializer.Serialize(answerRequest), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/v1/review/interact", jsonContent, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ответ на отзыв Ozon {Id} отправлен.", reviewId);
                }
                else
                {
                    _logger.LogError("Ошибка отправки в Ozon: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке ответа в Ozon.");
            }
        }
    }
}