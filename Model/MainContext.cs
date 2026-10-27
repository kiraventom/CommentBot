using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CommentBot.Model;

public class MainContext : DbContext
{
    public DbSet<ChannelModel> Channels { get; set; }
    public DbSet<PostModel> Posts { get; set; }
    public DbSet<FileModel> Files { get; set; }
    public DbSet<BotCommentModel> BotComments { get; set; }
    public DbSet<UserModel> Users { get; set; }
    public DbSet<UserCommentModel> UserComments { get; set; }
    public DbSet<BotReplyToUserCommentModel> BotReplies { get; set; }

    public MainContext(DbContextOptions<MainContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseLazyLoadingProxies(false)
            .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .EnableSensitiveDataLogging(true)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChannelModel>(p => 
        {
            p.HasIndex(p => p.TelegramId).IsUnique();
        });

        modelBuilder.Entity<PostModel>(p => 
        {
            p.HasIndex(p => p.TelegramId).IsUnique();
            p.HasOne<ChannelModel>().WithMany().HasForeignKey(p => p.ChannelId);
        });

        modelBuilder.Entity<FileModel>(p => 
        {
            p.HasOne<PostModel>().WithMany().HasForeignKey(f => f.PostId);
        });

        modelBuilder.Entity<BotCommentModel>(p => 
        {
            p.HasIndex(p => p.TelegramId).IsUnique();
            p.HasOne<PostModel>().WithOne().HasForeignKey<BotCommentModel>(c => c.PostId);
        });

        modelBuilder.Entity<UserModel>(p => 
        {
            p.HasIndex(p => p.TelegramId).IsUnique();
        });

        modelBuilder.Entity<UserCommentModel>(p => 
        {
            p.HasIndex(p => p.TelegramId).IsUnique();
            p.HasOne<UserModel>().WithMany().HasForeignKey(c => c.UserId);
            p.HasOne<PostModel>().WithMany().HasForeignKey(c => c.PostId);
            p.HasOne<BotCommentModel>().WithOne().HasForeignKey<UserCommentModel>(c => c.BotCommentId);
        });

        modelBuilder.Entity<BotReplyToUserCommentModel>(p => 
        {
            p.HasIndex(p => p.TelegramId).IsUnique();
            p.HasOne<PostModel>().WithMany().HasForeignKey(c => c.PostId);
            p.HasOne<UserCommentModel>().WithOne().HasForeignKey<BotReplyToUserCommentModel>(c => c.UserCommentId);
        });
    }
}

public class ChannelModel
{
    [Key] public int ChannelId { get; set; }

    public long TelegramId { get; set; }
    [Required] public string Title { get; set; }
}

public class PostModel
{
    [Key] public int PostId { get; set; }

    public int ChannelId { get; set; }

    public int TelegramId { get; set; }
    [Required] public DateTime DateTime { get; set; }
    public string Caption { get; set; }
}

public class FileModel
{
    [Key] public int FileId { get; set; }

    public int PostId { get; set; }
    [Required] public string TelegramFileId { get; set; }
}

public class BotCommentModel
{
    [Key] public int BotCommentId { get; set; }

    public int PostId { get; set; }

    public int TelegramId { get; set; }
    [Required] public string Text { get; set; }
}

public class UserModel
{
    [Key] public int UserId { get; set; }

    [Required] public string FirstName { get; set; }
    public long TelegramId { get; set; }
}

public class UserCommentModel
{
    [Key] public int UserCommentId { get; set; }

    public int UserId { get; set; }
    public int PostId { get; set; }
    public int BotCommentId { get; set; }

    public int TelegramId { get; set; }
    [Required] public string Text { get; set; }
}

public class BotReplyToUserCommentModel
{
    [Key] public int BotReplyToUserCommentId { get; set; }

    public int PostId { get; set; }
    public int UserCommentId { get; set; }

    public int TelegramId { get; set; }
    [Required] public string Text { get; set; }
}
