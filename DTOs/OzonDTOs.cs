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
        [JsonPropertyName("result")]
        public OzonResult Result { get; set; } = new();
    }

    public class OzonResult
    {
        [JsonPropertyName("reviews")]
        public List<OzonReview> Reviews { get; set; } = [];
    }

    public class OzonReview
    {
        [JsonPropertyName("uuid")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public OzonReviewText Text { get; set; } = new();

        [JsonPropertyName("product")]
        public OzonProductInfo Product { get; set; } = new();
    }

    public class OzonReviewText
    {
        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [JsonPropertyName("positive_comment")]
        public string Positive { get; set; } = string.Empty;

        [JsonPropertyName("negative_comment")]
        public string Negative { get; set; } = string.Empty;
    }
    
    public class OzonProductInfo
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }

    // --- ОТПРАВКА ОТВЕТА ---
    public class OzonAnswerRequest
    {
        [JsonPropertyName("review_uuid")]
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