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

            // Базовый адрес. Заголовки теперь ставим динамически в каждом запросе.
            _httpClient.BaseAddress = new Uri("https://api-seller.ozon.ru");
        }

        public async Task CheckForNewReviewsAsync(CancellationToken cancellationToken)
        {
            // Проверяем, есть ли хоть один аккаунт
            if (_apiKeys.OzonAccounts == null || _apiKeys.OzonAccounts.Count == 0)
            {
                _logger.LogWarning("Список аккаунтов Ozon пуст в настройках. Пропускаем.");
                return;
            }

            // ЦИКЛ ПО ВСЕМ АККАУНТАМ
            foreach (var account in _apiKeys.OzonAccounts)
            {
                if (string.IsNullOrWhiteSpace(account.ClientId) || string.IsNullOrWhiteSpace(account.ApiKey))
                {
                    _logger.LogWarning("Найден аккаунт Ozon с пустыми ключами. Пропускаем.");
                    continue;
                }

                _logger.LogInformation("--- Проверяем Ozon кабинет (Client-Id: {ClientId}) ---", account.ClientId);
                await ProcessAccountAsync(account, cancellationToken);
            }
        }

        private async Task ProcessAccountAsync(OzonAccountCredentials account, CancellationToken cancellationToken)
        {
            try
            {
                var requestDTO = new OzonReviewListRequest();
                // Используем наш метод отправки с авторизацией
                var response = await SendJsonRequestAsync("/v1/review/list", requestDTO, account, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ошибка Ozon API ({ClientId}): {StatusCode}", account.ClientId, response.StatusCode);
                    return;
                }

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var ozonData = await JsonSerializer.DeserializeAsync<OzonReviewListResponse>(stream, cancellationToken: cancellationToken);

                if (ozonData?.Reviews.Count == 0)
                {
                    _logger.LogInformation("Новых отзывов нет.");
                    return;
                }

                _logger.LogInformation("Найдено {Count} новых отзывов.", ozonData.Reviews.Count);

                foreach (var review in ozonData.Reviews)
                {
                    await ProcessReviewAsync(review, account, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке кабинета {ClientId}", account.ClientId);
            }
        }

        private async Task ProcessReviewAsync(OzonReview review, OzonAccountCredentials account, CancellationToken cancellationToken)
        {
            // Текст теперь приходит одной строкой
            var fullText = review.Text;

            if (string.IsNullOrWhiteSpace(fullText))
            {
                _logger.LogWarning("Отзыв Ozon ID: {Id} пустой. Отправляем заглушку.", review.Id);
                await SendAnswerAsync(review.Id, "Благодарим за высокую оценку!", account, cancellationToken);
                return;
            }

            var aiResponse = await _openAiClient.GetResponseForFeedback(fullText, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                _logger.LogWarning("OpenAI не ответил на отзыв {Id}", review.Id);
                return;
            }

            _logger.LogInformation("Для отзыва '{Text}' сгенерирован ответ: '{Answer}'", fullText.Replace("\n", "|"), aiResponse);

            await SendAnswerAsync(review.Id, aiResponse, account, cancellationToken);
        }

        private async Task SendAnswerAsync(string reviewId, string text, OzonAccountCredentials account, CancellationToken cancellationToken)
        {
            try
            {
                var answerRequest = new OzonAnswerRequest(reviewId, text);
                var response = await SendJsonRequestAsync("/v1/review/comment/create", answerRequest, account, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ответ на отзыв Ozon {Id} отправлен.", reviewId);
                }
                else
                {
                    _logger.LogError("Ошибка отправки в Ozon ({ClientId}): {StatusCode}", account.ClientId, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке ответа.");
            }
        }

        // Вспомогательный метод для создания запроса с правильными заголовками
        private async Task<HttpResponseMessage> SendJsonRequestAsync(string uri, object content, OzonAccountCredentials account, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(content);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            
            // Добавляем заголовки КОНКРЕТНОГО аккаунта
            requestMessage.Headers.Add("Client-Id", account.ClientId);
            requestMessage.Headers.Add("Api-Key", account.ApiKey);
            
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
    }
}