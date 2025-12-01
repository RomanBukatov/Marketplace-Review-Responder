using System.Text.Json.Serialization;

namespace WbAutoresponder.DTOs.Ozon
{
    // --- ЗАПРОС СПИСКА ОТЗЫВОВ ---
    public class OzonReviewListRequest
    {
        [JsonPropertyName("filter")]
        public OzonReviewFilter Filter { get; set; } = new();

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 20;

        [JsonPropertyName("sort_dir")]
        public string SortDir { get; set; } = "DESC"; // Свежие сверху
    }

    public class OzonReviewFilter
    {
        [JsonPropertyName("interaction_status")]
        public string InteractionStatus { get; set; } = "NOT_REPLIED"; // Только неотвеченные
    }

    // --- ОТВЕТ СО СПИСКОМ ОТЗЫВОВ ---
    public class OzonReviewListResponse
    {
        [JsonPropertyName("reviews")]
        public List<OzonReview> Reviews { get; set; } = [];

        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }
    }

    // Класс OzonResult больше не нужен, удали его.

    public class OzonReview
    {
        [JsonPropertyName("id")] // ИСПРАВЛЕНО: было uuid, стало id
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("product")]
        public OzonProductInfo Product { get; set; } = new();
    }

    public class OzonProductInfo
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }

    // --- ОТПРАВКА ОТВЕТА ---
    public class OzonAnswerRequest
    {
        [JsonPropertyName("review_id")]
        public string ReviewId { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        public OzonAnswerRequest(string reviewId, string text)
        {
            ReviewId = reviewId;
            Text = text;
        }
    }
}