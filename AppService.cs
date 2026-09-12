using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CommentBot;

public class AppService(ILogger<AppService> logger, ITelegramBotClient client, PostBuilder combiner, OpenRouterManager openRouterManager, IServiceScopeFactory spf) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        combiner?.PostCombined += OnPostCombined;

        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = [UpdateType.Message]
        };

        client.StartReceiving(OnUpdate, OnError, receiverOptions, cancellationToken: stoppingToken);
        logger.LogInformation("Started listening");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnPostCombined(Post post)
    {
        using var scope = spf.CreateScope();
        var imageDownloader = scope.ServiceProvider.GetRequiredService<ImageDownloader>();

        var caption = post.Caption;
        var imagePaths = new List<string>();
        foreach (var fileId in post.FileIds)
        {
            var imagePath = await imageDownloader.Download(fileId);
            if (imagePath is null)
                continue;

            imagePaths.Add(imagePath);
        }

        var comment = await openRouterManager.GetComment(caption, imagePaths);
        if (comment is null)
            return;

        var messageSender = scope.ServiceProvider.GetRequiredService<MessageSender>();
        await messageSender.Send(post.ChatId, post.MessageId, comment);
    }

    private async Task OnUpdate(ITelegramBotClient client, Update update, CancellationToken token)
    {
        using var scope = spf.CreateScope();
        var updateHandler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
        await updateHandler.HandleUpdate(update);
    }

    private Task OnError(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
    {
        logger.LogError(exception, "Polling error");
        return Task.CompletedTask;
    }
}

