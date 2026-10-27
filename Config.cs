using System.Text.Json;

namespace CommentBot;

public class Config(string botToken, string botUsername, string openRouterToken, string model, string systemPrompt, string postPrompt, string connectionString, string replyPrompt = null, string proxyAddress = null)
{
    /// <summary>
    /// Telegram bot token. Received from <a href="https://t.me/BotFather">BotFather</a>
    /// </summary>
    public string BotToken { get; } = botToken;

    /// <summary>
    /// Telegram username, without @
    /// </summary>
    public string BotUsername { get; } = botUsername;
    
    /// <summary>
    /// OpenRouter token
    /// </summary>
    public string OpenRouterToken { get; } = openRouterToken;

    /// <summary>
    /// OpenRouter model
    /// </summary>
    public string Model { get; } = model;

    /// <summary>
    /// System prompt.
    /// </summary>
    public string SystemPrompt { get; } = systemPrompt;

    /// <summary>
    /// Post prompt.
    /// </summary>
    public string PostPrompt { get; } = postPrompt;

    /// <summary>
    /// Reply prompt.
    /// </summary>
    public string ReplyPrompt { get; } = replyPrompt;

    /// <summary>
    /// SQLite database connection string
    /// </summary>
    public string ConnectionString { get; } = connectionString;

    /// <summary>
    /// HTTP proxy address. Optional
    /// </summary>
    public string ProxyAddress { get; } = proxyAddress;

    public static Config Load(string filepath)
    {
        using var configFile = File.OpenRead(filepath);
        return JsonSerializer.Deserialize<Config>(configFile, Common.JsonOptions);
    }

    public void Save(string filepath)
    {
        using var configFile = File.Create(filepath);
        JsonSerializer.Serialize<Config>(configFile, this, Common.JsonOptions);
    }
}

