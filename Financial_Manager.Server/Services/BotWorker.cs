using Financial_Manager.Server.Services;

public class BotWorker : BackgroundService
{
    private readonly TelegramBotService _botService;

    public BotWorker(TelegramBotService botService)
    {
        _botService = botService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _botService.StartReceivingUpdates();

        return Task.CompletedTask;
    }
}