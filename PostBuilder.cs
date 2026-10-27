using Microsoft.Extensions.Logging;

namespace CommentBot;

public record struct Media(DateTimeOffset UtcNow, string MediaGroupId, long ChatId, int MessageId, string FileId, string Text = null);

public class Post
{
    public long ChatId { get; }
    public string Caption { get; }
    public int MessageId { get; }
    public IReadOnlyCollection<string> FileIds { get; }
    public DateTime UtcDateTime { get; }

    public Post(DateTime utcDateTime, IReadOnlyCollection<Media> medias)
    {
        ChatId = medias.First().ChatId;
        MessageId = medias.Min(m => m.MessageId);
        Caption = string.Join('\n', medias.Select(m => m.Text).Where(t => !string.IsNullOrEmpty(t)).ToList());
        FileIds = medias.Select(m => m.FileId).ToList();
        UtcDateTime = utcDateTime;
    }

    public override string ToString()
    {
        return $"{{ MessageId=\"{MessageId}\", Caption=\"{Caption}\", FileIds={FileIds.Count} }}";
    }
}

public class PostBuilder
{
    private const int LOOP_TIMEOUT = 5;

    private readonly ILogger<PostBuilder> logger;
    private readonly SortedDictionary<DateTimeOffset, Media> _mediasDict = new(Comparer<DateTimeOffset>.Create((a, b) => b.CompareTo(a)));
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public event Func<Post, Task> PostCombined;

    public PostBuilder(ILogger<PostBuilder> logger)
    {
        this.logger = logger;

        _ = StartCollectingPosts();
    }

    public void AddPhoto(long chatId, int messageId, string fileId, string mediaGroupId, string text)
    {
        _semaphore.Wait();

        try
        {
            var utcNow = DateTimeOffset.UtcNow;
            var media = new Media(utcNow, mediaGroupId, chatId, messageId, fileId, text);
            _mediasDict.Add(utcNow, media);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task StartCollectingPosts()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(LOOP_TIMEOUT));

            if (_mediasDict.Count == 0)
                continue;

            var utcDT = DateTime.UtcNow;
            var lastMediaDT = _mediasDict.Keys.First();

            if ((utcDT - lastMediaDT).TotalSeconds < LOOP_TIMEOUT)
                continue;

            await _semaphore.WaitAsync();

            List<Media> medias = [];
            try
            {
                var lastMedia = _mediasDict[lastMediaDT];
                medias.AddRange(_mediasDict.Values.Where(m => m.MediaGroupId == lastMedia.MediaGroupId));

                var post = new Post(utcDT, medias);
                logger.LogInformation("Post {post} successfully combined", post.ToString());

                if (PostCombined != null)
                    await PostCombined.Invoke(post);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception during building a post");
            }
            finally
            {
                foreach (var dt in medias.Select(m => m.UtcNow))
                    _mediasDict.Remove(dt);

                _semaphore.Release();
            }
        }
    }
}

