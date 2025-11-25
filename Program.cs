using WbAutoresponder;
using OpenAI.GPT3.Extensions;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);
// Регистрируем наши классы конфигурации
builder.Services.Configure<WbAutoresponder.Configuration.ApiKeys>(builder.Configuration.GetSection("ApiKeys"));
builder.Services.Configure<WbAutoresponder.Configuration.WorkerSettings>(builder.Configuration.GetSection("WorkerSettings"));

// Регистрируем HTTP клиент для Wildberries API
builder.Services.AddHttpClient<WbAutoresponder.Services.IWildberriesApiClient, WbAutoresponder.Services.WildberriesApiClient>();
// Регистрируем HTTP клиент для Ozon API
builder.Services.AddHttpClient<WbAutoresponder.Services.IOzonApiClient, WbAutoresponder.Services.OzonApiClient>();
// Регистрируем HTTP клиент для OpenAI API
builder.Services.AddOpenAIService(settings =>
{
    settings.ApiKey = builder.Configuration["ApiKeys:OpenAI"] ?? 
                      throw new InvalidOperationException("OpenAI API key is not configured.");
});
builder.Services.AddTransient<WbAutoresponder.Services.IOpenAiClient, WbAutoresponder.Services.OpenAiClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
