using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace CommentBot;

public class UpdateHandler(ILogger<UpdateHandler> logger)
{
    // From ID of channel posts in discussion group
    private const long ID = 777000;

    public async Task HandleUpdate(Update update)
    {
        if (update.Message is not {} message)
            return;

        if (message.From.Id != ID)
            return;

        var photo = message.Photo;
        if (photo is null)
            return;

        var caption = message.Caption;

        if (message.MediaGroupId is not null)
        {
            // TODO
        }

        // TODO
    }
}

