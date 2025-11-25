namespace WbAutoresponder.Services
{
    public interface IOzonApiClient
    {
        Task CheckForNewReviewsAsync(CancellationToken cancellationToken);
    }
}