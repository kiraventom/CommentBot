using Microsoft.EntityFrameworkCore;

namespace CommentBot.Model.Factory;

public class MainContextFactory : DesignTimeContextFactory<MainContext>
{
    public override MainContext CreateDbContext(string[] args)
    {
        var config = LoadConfig(args);
        var builder = new DbContextOptionsBuilder<MainContext>();
        builder.UseSqlite(config.ConnectionString);

        return new MainContext(builder.Options);
    }
}
