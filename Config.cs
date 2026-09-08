using System.Text.Json;

namespace CommentBot;

public class Config(string botToken, string botUsername, string openRouterToken, string model, string prompt, string proxyAddress = null)
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
    public string Prompt { get; } = prompt;

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

