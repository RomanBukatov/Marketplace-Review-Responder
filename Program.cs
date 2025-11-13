using WbAutoresponder;

var builder = Host.CreateApplicationBuilder(args);
// Регистрируем наши классы конфигурации
builder.Services.Configure<WbAutoresponder.Configuration.ApiKeys>(builder.Configuration.GetSection("ApiKeys"));
builder.Services.Configure<WbAutoresponder.Configuration.WorkerSettings>(builder.Configuration.GetSection("WorkerSettings"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
