using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CommentBot;

public class MessageSender(ILogger<MessageSender> logger, ITelegramBotClient client)
{
    public async Task Dislike(long chatId, int messageId)
    {
        ReactionType[] reaction = [ new ReactionTypeEmoji() { Emoji = "👎" } ];
        await client.SetMessageReaction(chatId, messageId, reaction);
    }

    public async Task<int?> Send(long chatId, int replyToId, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            logger.LogWarning("Empty text message skipped");
            return null;
        }
        
        var message = await client.SendMessage(
            chatId: chatId,
            text: text, 
            replyParameters: new ReplyParameters() { MessageId = replyToId });

        return message.MessageId;
    }
}

