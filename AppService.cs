using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CommentBot;

public class AppService(ILogger<AppService> logger, ITelegramBotClient client, UpdateHandler updateHandler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = [UpdateType.ChannelPost, UpdateType.Message]
        };

        client.StartReceiving(OnUpdate, OnError, receiverOptions, cancellationToken: stoppingToken);
        logger.LogInformation("Started listening");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnUpdate(ITelegramBotClient client, Update update, CancellationToken token)
    {
        await updateHandler.HandleUpdate(update);
    }

    private Task OnError(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
    {
        logger.LogError(exception, "Polling error");
        return Task.CompletedTask;
    }
}

