using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace CommentBot;

public class MessageSender(ILogger<MessageSender> logger, ITelegramBotClient client)
{
    public async Task Send(long chatId, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            logger.LogWarning("Empty text message skipped");
            return;
        }
        
        await client.SendMessage(
            chatId: chatId,
            text: text);
    }
}

