using CommentBot.Model;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace CommentBot;

// TODO Split into PostHandler/CommentHandler
public class UpdateHandler(ILogger<UpdateHandler> logger, Config config, MainContext context, PostBuilder postBuilder, OpenRouterManager openRouterManager, ImageDownloader imageDownloader, MessageSender messageSender)
{
    // From ID of channel posts in discussion group
    private const long CHANNEL_ID = 777000;

    public async Task HandleUpdate(Update update, CancellationToken token)
    {
        if (update.Message is not {} message)
            return;

        var transaction = await context.Database.BeginTransactionAsync(token);

        try
        {
            if (message.From is { Id: CHANNEL_ID })
                await HandlePost(message);
            else if (!string.IsNullOrEmpty(config.ReplyPrompt))
                await HandleReply(message);

            await context.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(token);
            logger.LogError(e, "Transaction failed, rolling back");
        }
    }

    private async Task HandlePost(Message message)
    {
        var photos = message.Photo;
        if (photos is null)
            return;

        var chatId = message.Chat.Id;
        if (context.Channels.All(c => c.TelegramId != chatId))
        {
            var channel = new ChannelModel() 
            {
                TelegramId = chatId,
                Title = message.Chat.Title,
            };

            context.Channels.Add(channel);
        }

        var caption = message.Caption;

        const int SIZE_LIMIT = 100_000;
        var photo = photos.OrderByDescending(p => p.FileSize).SkipWhile(p => p.FileSize > SIZE_LIMIT).FirstOrDefault();

        if (photo is null)
        {
            logger.LogWarning("Failed to find photo size smaller than {sizeLimit}, available sizes: [ {sizes} ]", SIZE_LIMIT, string.Join(", ", photos.Select(p => p.FileSize.ToString())));

            return;
        }

        if (string.IsNullOrEmpty(caption))
            logger.LogInformation("New update: photo");
        else
            logger.LogInformation("New post: \"{caption}\", photo", caption);

        var mediaGroupId = message.MediaGroupId ?? message.MessageId.ToString();

        postBuilder.AddPhoto(message.Chat.Id, message.MessageId, photo.FileId, mediaGroupId, caption);
    }

    private async Task HandleReply(Message message)
    {
        if (message.ReplyToMessage is not {} replyToMessage)
            return;

        if (message.From is null)
            return;

        var userId = message.From.Id;
        var user = context.Users.FirstOrDefault(c => c.TelegramId == userId);
        if (user is null)
        {
            user = new UserModel() 
            { 
                TelegramId = userId,
                FirstName = message.From.FirstName
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        if (string.IsNullOrWhiteSpace(message.Text))
            return;

        var channel = context.Channels.FirstOrDefault(c => c.TelegramId == message.Chat.Id);
        if (channel is null)
            return;

        var posts = context.Posts.Where(c => c.ChannelId == channel.ChannelId);
        var botComments = posts.Select(p =>
                new { Post = p, BotComment = context.BotComments.FirstOrDefault(c => c.PostId == p.PostId) })
            .Where(x => x.BotComment != null)
            .Select(x => x.BotComment);

        var botComment = botComments.FirstOrDefault(c => c.TelegramId == replyToMessage.MessageId);

        if (botComment is null)
            return;

        var post = context.Posts.First(p => p.PostId == botComment.PostId);

        logger.LogInformation("Post {postId}, comment {commentId}: Received comment from user {userId} \"{name}\", text \"{text}\"", post.PostId, botComment.BotCommentId, user.UserId, user.FirstName, message.Text);

        var userComment = context.UserComments.FirstOrDefault(c => c.PostId == post.PostId && c.UserId == user.UserId);
        if (userComment is not null)
        {
            logger.LogInformation("Post {postId}, comment {commentId}: Already replied to user {userId} \"{name}\", ignoring", post.PostId, botComment.BotCommentId, user.UserId, user.FirstName);

            return;
        }

        userComment = new UserCommentModel()
        {
            PostId = post.PostId,
            BotCommentId = botComment.BotCommentId,
            UserId = user.UserId,
            TelegramId = message.MessageId,
            Text = message.Text
        };

        context.UserComments.Add(userComment);
        await context.SaveChangesAsync();

        var fileIds = context.Files
            .Where(f => f.PostId == post.PostId)
            .Select(f => f.TelegramFileId);
        
        var imagePaths = await imageDownloader.DownloadAll(fileIds).ToListAsync();
        
        var reply = await openRouterManager.GetReply(post.Caption, imagePaths, botComment.Text, new UserComment(user.FirstName, userComment.Text));

        if (string.IsNullOrEmpty(reply))
        {
            logger.LogWarning("OpenRouter returned empty string");
            await messageSender.Dislike(channel.TelegramId, userComment.TelegramId);
            return;
        }

        var replyTelegramId = await messageSender.Send(channel.TelegramId, userComment.TelegramId, reply);
        if (replyTelegramId is null)
            return;

        var replyModel = new BotReplyToUserCommentModel()
        {
            PostId = post.PostId,
            TelegramId = replyTelegramId.Value,
            UserCommentId = userComment.UserCommentId,
            Text = reply
        };

        context.BotReplies.Add(replyModel);
    }
}

