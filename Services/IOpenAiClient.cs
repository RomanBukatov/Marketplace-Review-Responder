namespace WbAutoresponder.Services
{
    public interface IOpenAiClient
    {
        Task<string?> GetResponseForFeedback(string feedbackText, CancellationToken cancellationToken);
    }
}