using System.Text.Json.Serialization;

namespace WbAutoresponder.DTOs
{
    public class WbFeedbackPatchRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        public WbFeedbackPatchRequest(string id, string text)
        {
            Id = id;
            Text = text;
        }
    }
}