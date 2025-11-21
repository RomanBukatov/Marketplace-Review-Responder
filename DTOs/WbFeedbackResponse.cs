using System.Text.Json.Serialization;

namespace WbAutoresponder.DTOs
{
    // Основной контейнер ответа
    public class WbFeedbackResponse
    {
        [JsonPropertyName("data")]
        public FeedbackData Data { get; set; } = new();
    }

    public class FeedbackData
    {
        [JsonPropertyName("feedbacks")]
        public List<Feedback> Feedbacks { get; set; } = [];
    }

    // Модель самого отзыва
    public class Feedback
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("pros")]
        public string Pros { get; set; } = string.Empty;

        [JsonPropertyName("cons")]
        public string Cons { get; set; } = string.Empty;

        [JsonPropertyName("productDetails")]
        public ProductDetails ProductDetails { get; set; } = new();

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }
    }

    // Модель информации о товаре
    public class ProductDetails
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;
    }
}