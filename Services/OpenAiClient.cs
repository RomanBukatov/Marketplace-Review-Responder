using Microsoft.Extensions.Options;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using WbAutoresponder.Configuration;

namespace WbAutoresponder.Services
{
    public class OpenAiClient : IOpenAiClient
    {
        private readonly IOpenAIService _openAiService;
        private readonly ILogger<OpenAiClient> _logger;

        public OpenAiClient(IOpenAIService openAiService, ILogger<OpenAiClient> logger)
        {
            _openAiService = openAiService;
            _logger = logger;
        }

        public async Task<string?> GetResponseForFeedback(string feedbackText, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запрашиваем ответ от OpenAI...");

            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages =
                [
                    // Системный промпт, который задает роль и стиль ответа
                    ChatMessage.FromSystem(
                        "Ты — профессиональный, вежливый и эмпатичный менеджер по работе с клиентами магазина на Wildberries. " +
                        "Твоя задача — написать лаконичный и человечный ответ на отзыв клиента. " +
                        "Строго следуй этим правилам:\n" +
                        "1. Ответ должен быть 2-3 предложения.\n" +
                        "2. Не используй роботизированные фразы. Будь дружелюбным, но профессиональным.\n" +
                        "3. Если отзыв положительный, поблагодари за высокую оценку.\n" +
                        "4. Если отзыв негативный, извинись за негативный опыт и вежливо сообщи, что вы разберетесь в ситуации. Никогда не предлагай скидки или возврат денег.\n\n" +
                        "Вот пример идеального ответа на положительный отзыв:\n" +
                        "Отзыв клиента: \"Все супер, кроссовки огонь, доставка быстрая!\"\n" +
                        "Твой ответ: \"Здравствуйте! Большое спасибо за ваш отзыв и высокую оценку. Мы очень рады, что вам все понравилось. Носите с удовольствием!\""
                    ),
                    // Сообщение пользователя (текст отзыва)
                    ChatMessage.FromUser(feedbackText)
                ],
                Model = OpenAI.GPT3.ObjectModels.Models.ChatGpt3_5Turbo,
                MaxTokens = 150 // Ограничиваем длину ответа
            }, cancellationToken: cancellationToken);

            if (completionResult.Successful && completionResult.Choices.Any())
            {
                var responseText = completionResult.Choices.First().Message.Content;
                _logger.LogInformation("Ответ от OpenAI получен.");
                return responseText;
            }

            _logger.LogError("Ошибка при получении ответа от OpenAI: {Error}", completionResult.Error?.Message);
            return null;
        }
    }
}