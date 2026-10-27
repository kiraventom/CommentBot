using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace CommentBot;

public class ImageDownloader(ILogger<ImageDownloader> logger, ITelegramBotClient client)
{
    public async Task<string> Download(string fileId)
    {
        var tgFile = await client.GetFile(fileId);

        if (tgFile.FilePath is null)
        {
            logger.LogError("{fileId} has null FilePath", fileId);
            return null;
        }

        var localFileName = Path.GetTempFileName();
        using var fs = File.OpenWrite(localFileName);
        await client.DownloadFile(tgFile.FilePath, fs);

        return localFileName;
    }

    public async IAsyncEnumerable<string> DownloadAll(IEnumerable<string> fileIds)
    {
        foreach (var fileId in fileIds)
        {
            var imagePath = await Download(fileId);
            if (imagePath is null)
                continue;

            yield return imagePath;
        }
    }
}


