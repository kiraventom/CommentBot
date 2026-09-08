using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace CommentBot;

public class Media
{
    public string FileId { get; }
}

public class MediaCombiner : IDisposable
{
    private readonly Config config;
    private readonly ILogger<InputListener> logger;
    private readonly List<Media> _media = new();
    private readonly object _lock = new();
    private readonly Timer _timer;
    private long _batchUserId;

    public MediaCombiner(Config config, ILogger<InputListener> logger)
    {
        this.config = config;
        this.logger = logger;
        _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
    }


    public void AddMedia(Media media)
    {
        lock (_lock)
        {
            // TODO
            _media.Add(media);
            _timer.Change(5000, Timeout.Infinite);
        }
    }

    private async void TimerCallback(object state)
    {
        // TODO
    }

    public void Dispose() => _timer?.Dispose();
}
