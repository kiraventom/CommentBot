using CommentBot.Model;
using Microsoft.EntityFrameworkCore;
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
        using var scope = spf.CreateScope();
        using (var context = scope.ServiceProvider.GetRequiredService<MainContext>())
        {
            context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            await context.Database.MigrateAsync();
        }

        combiner.PostCombined += OnPostCombined;

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
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var imageDownloader = scope.ServiceProvider.GetRequiredService<ImageDownloader>();
            var caption = post.Caption;

            var channel = context.Channels.First(c => c.TelegramId == post.ChatId);
            var postModel = new PostModel()
            {
                TelegramId = post.MessageId,
                Caption = caption,
                DateTime = post.UtcDateTime,
                ChannelId = channel.ChannelId
            };

            context.Posts.Add(postModel);
            await context.SaveChangesAsync();

            var imagePaths = new List<string>();
            foreach (var fileId in post.FileIds)
            {
                var imagePath = await imageDownloader.Download(fileId);
                if (imagePath is null)
                    continue;

                imagePaths.Add(imagePath);

                var fileModel = new FileModel()
                {
                    TelegramFileId = fileId,
                    PostId = postModel.PostId,
                };

                context.Files.Add(fileModel);
            }

            var comment = await openRouterManager.GetComment(caption, imagePaths);

            if (comment is null)
            {
                logger.LogWarning("OpenRouter returned empty string");
                return;
            }

            var messageSender = scope.ServiceProvider.GetRequiredService<MessageSender>();
            var sentMessageId = await messageSender.Send(post.ChatId, post.MessageId, comment);

            if (sentMessageId is null)
                return;

            var commentModel = new BotCommentModel()
            {
                TelegramId = sentMessageId.Value,
                PostId = postModel.PostId,
                Text = comment
            };

            context.BotComments.Add(commentModel);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, "Transaction failed, rolling back");
        }
    }

    private async Task OnUpdate(ITelegramBotClient client, Update update, CancellationToken token)
    {
        using var scope = spf.CreateScope();
        var updateHandler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
        await updateHandler.HandleUpdate(update, token);
    }

    private Task OnError(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
    {
        logger.LogError(exception, "Polling error");
        return Task.CompletedTask;
    }
}

