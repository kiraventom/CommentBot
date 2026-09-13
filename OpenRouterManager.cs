using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CommentBot;

public class OpenRouterManager : IDisposable
{
    private record OpenRouterResponse(List<Choice> choices, Usage usage);
    private record Choice(Message message, string finish_reason);
    private record Usage(double cost);
    private record Message(string content, string refusal);

    private HttpClient Client { get; }

    private const string OPENROUTER_URL = @"https://openrouter.ai/api/v1/chat/completions";
    private readonly ILogger<OpenRouterManager> logger;
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

    public async Task<string> GetComment(string caption, IReadOnlyCollection<string> imagePaths)
    {
        var systemPrompt = config.Prompt;

        var userContent = new List<object>();

        if (!string.IsNullOrEmpty(caption))
        {
            userContent.Add(new { type = "text", text = caption });
        }

        foreach (var path in imagePaths)
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(path);
            string base64 = Convert.ToBase64String(imageBytes);

            userContent.Add(new
            {
                type = "image_url",
                image_url = new
                {
                    url = $"data:image/jpeg;base64,{base64}",
                    detail = "auto"
                }
            });
        }

        var payload = new
        {
            model = config.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            }
        };

        var payloadStr = JsonSerializer.Serialize(payload);

        logger.LogInformation("Sending payload: {str} chars", payloadStr.Length);

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

        var responseText = choice.message.content;

        logger.LogInformation("OpenRouter returned \"{response}\"", choice.message.content);

        return responseText;
    }
}

