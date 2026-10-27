using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CommentBot;

public record UserComment(string Author, string Text);

public class OpenRouterManager : IDisposable
{
    private record OpenRouterResponse(List<Choice> choices, Usage usage);
    private record Choice(Message message, string finish_reason);
    private record Usage(double cost);
    private record Message(string content, string refusal);

    private const string OPENROUTER_URL = @"https://openrouter.ai/api/v1/chat/completions";

    private HttpClient Client { get; }

    private readonly ILogger logger;
    private readonly Config config;

    public OpenRouterManager(ILogger<OpenRouterManager> logger, Config config)
    {
        this.logger = logger;
        this.config = config;

        if (string.IsNullOrEmpty(config.ProxyAddress))
        {
            Client = new HttpClient();
        }
        else
        {
            Client = new HttpClient(
                new SocketsHttpHandler()
                {
                    Proxy = new WebProxy(config.ProxyAddress),
                    UseProxy = true
                });
        }

        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.OpenRouterToken}");
    }

    public void Dispose() => Client.Dispose();

    public Task<string> GetComment(string caption, IReadOnlyCollection<string> imagePaths) =>
        Request(caption, imagePaths, previousComment: null, userComment: null);

    public Task<string> GetReply(
        string caption,
        IReadOnlyCollection<string> imagePaths,
        string botComment,
        UserComment userComment) =>
        Request(caption, imagePaths, botComment, userComment);

    private async Task<string> Request(
        string caption,
        IReadOnlyCollection<string> imagePaths,
        string previousComment = null,
        UserComment userComment = null)
    {
        var isReply = userComment is not null;

        var systemPrompt = config.SystemPrompt;
        if (isReply)
            systemPrompt += $"\n\n{config.ReplyPrompt}";
        else
            systemPrompt += $"\n\n{config.PostPrompt}";

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        var postContent = new List<object>();

        if (!string.IsNullOrEmpty(caption))
            postContent.Add(new { type = "text", text = caption });

        foreach (var path in imagePaths)
        {
            var imageBytes = await File.ReadAllBytesAsync(path);
            var base64 = Convert.ToBase64String(imageBytes);

            postContent.Add(new
            {
                type = "image_url",
                image_url = new
                {
                    url = $"data:image/jpeg;base64,{base64}",
                    detail = "auto"
                }
            });
        }

        if (postContent.Count > 0)
            messages.Add(new { role = "user", content = postContent });

        if (!string.IsNullOrWhiteSpace(previousComment))
            messages.Add(new { role = "assistant", content = previousComment });

        if (userComment is not null)
            messages.Add(new { role = "user", content = $"Комментарий от {userComment.Author}: {userComment.Text}" });

        if (messages.Count == 1)
        {
            logger.LogWarning("Nothing to send: post has no caption and no images");
            return null;
        }

        var payload = new
        {
            model = config.Model,
            messages
        };

        var payloadStr = JsonSerializer.Serialize(payload);

        logger.LogInformation("Sending payload: {str} chars, {count} messages", payloadStr.Length, messages.Count);

        var response = await Client.PostAsync(OPENROUTER_URL,
                new StringContent(payloadStr, Encoding.UTF8, "application/json"));

        foreach (var imagePath in imagePaths)
            File.Delete(imagePath);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("OpenRouter returned {statusCode}, reason: {reason}", response.StatusCode, response.ReasonPhrase);
            return null;
        }

        using var respStream = await response.Content.ReadAsStreamAsync();
        var deserResponse = await JsonSerializer.DeserializeAsync<OpenRouterResponse>(respStream);

        logger.LogInformation("Cost of reply: {cost}", deserResponse.usage.cost);

        if (deserResponse.choices.Count == 0)
        {
            logger.LogError("OpenRouter response contains 0 choices");
            return null;
        }

        var choice = deserResponse.choices[0];
        if (choice.finish_reason != "stop")
        {
            logger.LogError("OpenRouter finish reason is not \"stop\", but \"{reason}\"", choice.finish_reason);
            return null;
        }

        if (choice.message.refusal != null)
        {
            logger.LogError("OpenRouter refusal is not null, but \"{refusal}\"", choice.message.refusal);
            return null;
        }

        logger.LogInformation("OpenRouter returned \"{response}\"", choice.message.content);

        return choice.message.content;
    }
}
