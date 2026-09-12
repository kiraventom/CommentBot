using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CommentBot;

public class MessageSender(ILogger<MessageSender> logger, ITelegramBotClient client)
{
    public async Task Send(long chatId, int replyToId, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            logger.LogWarning("Empty text message skipped");
            return;
        }
        
        await client.SendMessage(
            chatId: chatId,
            text: text, 
            replyParameters: new ReplyParameters() { MessageId = replyToId });
    }
}

