namespace WbAutoresponder.Configuration
{
    public class ApiKeys
    {
        public string Wildberries { get; set; } = string.Empty;
        public string OpenAI { get; set; } = string.Empty;
        
        // Список аккаунтов Ozon
        public List<OzonAccountCredentials> OzonAccounts { get; set; } = [];
    }

    public class OzonAccountCredentials
    {
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}