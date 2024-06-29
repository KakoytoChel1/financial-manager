using Financial_Manager.Server.Hubs;
using Financial_Manager.Server.Model;
using Financial_Manager.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Register it in the dependency injection container
builder.Services.AddDbContext<ApplicationContext>();

// Adding data from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json");

builder.Services.Configure<TelegramBotSettings>
    (builder.Configuration.GetSection("TelegramBotSettings"));

builder.Services.AddSignalR();

// Registering the "TelegramBotService" to work with Telegram bot
builder.Services.AddSingleton<TelegramBotService>();

// Adding work with Telegram bot as a background task
builder.Services.AddHostedService<BotWorker>();

var app = builder.Build();

app.MapHub<MainHub>("/mainHub");

app.Run();
