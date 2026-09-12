using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace CommentBot;

public class UpdateHandler(ILogger<UpdateHandler> logger, PostBuilder postBuilder)
{
    // From ID of channel posts in discussion group
    private const long ID = 777000;

    public async Task HandleUpdate(Update update)
    {
        if (update.Message is not {} message)
            return;

        if (message.From.Id != ID)
            return;

        var photos = message.Photo;
        if (photos is null)
            return;

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
}

