namespace WbAutoresponder.Services
{
    public interface IWildberriesApiClient
    {
        Task CheckForNewReviewsAsync(CancellationToken cancellationToken);
    }
}