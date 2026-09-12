using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Telegram.Bot;

namespace CommentBot;

public record Paths(string ConfigDir, string DataDir);

public static class Program
{
    private const string PROJECT_NAME = nameof(CommentBot);

    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        Log.Information("Building host...");
        
        try
        {
            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddSerilog(ConfigureLogger)
                .AddSingleton<Paths>(BuildPaths)
                .AddSingleton<Config>(BuildConfig)
                .AddSingleton<ITelegramBotClient, TelegramBotClient>(ConfigureBotClient)
                .AddTransient<ImageDownloader>()
                .AddTransient<MessageSender>()
                .AddSingleton<PostBuilder>()
                .AddSingleton<OpenRouterManager>()
                .AddScoped<UpdateHandler>();

            builder.Services.AddHostedService<AppService>();

            var host = builder.Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to build host, terminating");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static TelegramBotClient ConfigureBotClient(IServiceProvider provider)
    {
        var config = provider.GetRequiredService<Config>();
        var telegramBotClientOptions = new TelegramBotClientOptions(config.BotToken);

        if (Uri.TryCreate(config.ProxyAddress, UriKind.Absolute, out Uri proxyUri))
        {
            Log.Logger.Information("Using proxy adress {address}", proxyUri);
            var webProxy = new System.Net.WebProxy(proxyUri);

            var httpClientHandler = new System.Net.Http.HttpClientHandler 
            { 
                Proxy = webProxy, 
                UseProxy = true 
            };

            var httpClient = new System.Net.Http.HttpClient(httpClientHandler);

            return new TelegramBotClient(telegramBotClientOptions, httpClient);        
        }

        Log.Logger.Information("Using no proxy address", proxyUri);

        return new TelegramBotClient(telegramBotClientOptions);

    }
    private static void ConfigureLogger(IServiceProvider provider, LoggerConfiguration configuration)
    {
        var paths = provider.GetRequiredService<Paths>();
        var logsDirPath = Path.Combine(paths.DataDir, "logs");
        Directory.CreateDirectory(logsDirPath);
        var logFilePath = Path.Combine(logsDirPath, $"{PROJECT_NAME}.log");
        
        const string template = @"[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        configuration.MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.File(logFilePath, outputTemplate: template, rollingInterval: RollingInterval.Day)
            .WriteTo.Console(outputTemplate: template, restrictedToMinimumLevel: LogEventLevel.Information);
    }

    private static Config BuildConfig(IServiceProvider provider)
    {
        var paths = provider.GetRequiredService<Paths>();
        var configFilePath = Path.Combine(paths.ConfigDir, "config.json");

        if (!File.Exists(configFilePath))
        {
            var defaultConfig = new Config("BOT_TOKEN", "USERNAME_BOT", "OPENROUTER_TOKEN", "openrouter/model-3.0", "Analyze the picture attached");
            defaultConfig.Save(configFilePath); 
            throw new InvalidOperationException($"Default config created at {configFilePath}. Fill it out and restart.");
        }

        return Config.Load(configFilePath);
    }

    private static Paths BuildPaths(IServiceProvider provider)
    {
        var curDir = Environment.CurrentDirectory;

        var configDirPath = Path.Combine(curDir, "config");
        var dataDirPath = Path.Combine(curDir, "data");

        Directory.CreateDirectory(configDirPath);
        Directory.CreateDirectory(dataDirPath);

        return new Paths(configDirPath, dataDirPath);
    }
}
