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
        
        // ПАМЯТЬ: Список обработанных ID
        private static readonly HashSet<string> _processedReviewIds = new();
        private const string HistoryFileName = "processed_reviews.txt";
        private static readonly object _fileLock = new();

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

            _httpClient.BaseAddress = new Uri("https://api-seller.ozon.ru");
            
            // При запуске загружаем историю, чтобы не отвечать повторно после перезагрузки
            LoadProcessedIds();
        }

        private void LoadProcessedIds()
        {
            lock (_fileLock)
            {
                if (!File.Exists(HistoryFileName)) return;
                
                try
                {
                    var lines = File.ReadAllLines(HistoryFileName);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            _processedReviewIds.Add(line.Trim());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось загрузить историю обработанных отзывов.");
                }
            }
        }

        private void SaveProcessedId(string id)
        {
            lock (_fileLock)
            {
                if (_processedReviewIds.Contains(id)) return;

                _processedReviewIds.Add(id);
                try
                {
                    File.AppendAllText(HistoryFileName, id + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось сохранить ID отзыва в историю.");
                }
            }
        }

        public async Task CheckForNewReviewsAsync(CancellationToken cancellationToken)
        {
            if (_apiKeys.OzonAccounts == null || _apiKeys.OzonAccounts.Count == 0)
            {
                _logger.LogWarning("Список аккаунтов Ozon пуст в настройках. Пропускаем.");
                return;
            }

            foreach (var account in _apiKeys.OzonAccounts)
            {
                if (string.IsNullOrWhiteSpace(account.ClientId) || string.IsNullOrWhiteSpace(account.ApiKey))
                {
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

                _logger.LogInformation("Найдено {Count} отзывов (включая уже обработанные).", ozonData.Reviews.Count);

                foreach (var review in ozonData.Reviews)
                {
                    // ЗАЩИТА ОТ ДУБЛЕЙ: Проверяем, есть ли ID в нашей памяти
                    if (_processedReviewIds.Contains(review.Id))
                    {
                        // _logger.LogInformation("Отзыв {Id} уже был обработан нами ранее. Пропускаем.", review.Id);
                        continue;
                    }

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
            var fullText = review.Text;

            // 1. ПУСТОЙ ОТЗЫВ
            if (string.IsNullOrWhiteSpace(fullText))
            {
                _logger.LogWarning("Отзыв Ozon ID: {Id} пустой. Отправляем заглушку.", review.Id);
                bool success = await SendAnswerAsync(review.Id, "Благодарим за высокую оценку!", account, cancellationToken);
                if (success) SaveProcessedId(review.Id); // Запоминаем!
                return;
            }

            // 2. ГЕНЕРАЦИЯ ОТВЕТА
            var aiResponse = await _openAiClient.GetResponseForFeedback(fullText, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                _logger.LogWarning("OpenAI не ответил на отзыв {Id}", review.Id);
                return;
            }

            _logger.LogInformation("Для отзыва '{Text}' сгенерирован ответ: '{Answer}'", fullText.Replace("\n", "|"), aiResponse);

            // 3. ОТПРАВКА
            bool sent = await SendAnswerAsync(review.Id, aiResponse, account, cancellationToken);
            
            // 4. ЗАПОМИНАЕМ, ЧТО ОТВЕТИЛИ
            if (sent)
            {
                SaveProcessedId(review.Id);
            }
        }

        private async Task<bool> SendAnswerAsync(string reviewId, string text, OzonAccountCredentials account, CancellationToken cancellationToken)
        {
            try
            {
                // Используем ПРАВИЛЬНОЕ имя поля review_id (как мы выяснили опытным путем)
                var answerRequest = new OzonAnswerRequest(reviewId, text);
                
                // Используем ПРАВИЛЬНЫЙ метод
                var response = await SendJsonRequestAsync("/v1/review/comment/create", answerRequest, account, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ответ на отзыв Ozon {Id} успешно отправлен.", reviewId);
                    return true;
                }
                else
                {
                    // Логируем ошибку, но не тело (чтобы не засорять)
                    _logger.LogError("Ошибка отправки в Ozon ({ClientId}): {StatusCode}", account.ClientId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при отправке ответа.");
                return false;
            }
        }

        private async Task<HttpResponseMessage> SendJsonRequestAsync(string uri, object content, OzonAccountCredentials account, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(content);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Headers.Add("Client-Id", account.ClientId);
            requestMessage.Headers.Add("Api-Key", account.ApiKey);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
    }
}