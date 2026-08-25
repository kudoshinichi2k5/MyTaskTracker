using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tracker.CommentService.Data;

public class CommentDbContextFactory
    : IDesignTimeDbContextFactory<
        CommentDbContext>
{
    public CommentDbContext CreateDbContext(
        string[] args)
    {
        var configuration =
            new ConfigurationBuilder()
                .SetBasePath(
                    Directory.GetCurrentDirectory())
                .AddJsonFile(
                    "appsettings.json",
                    optional: true)
                .AddJsonFile(
                    "appsettings.Development.json",
                    optional: true)
                .AddUserSecrets<
                    CommentDbContextFactory>(
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

        var connectionString =
            configuration.GetConnectionString(
                "CommentDb")
            ?? "Server=localhost;" +
               "Port=3306;" +
               "Database=tracker_comments;" +
               "User=comment_service;" +
               "Password=change_me_comment;";

        var optionsBuilder =
            new DbContextOptionsBuilder<
                CommentDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(
                connectionString));

        return new CommentDbContext(
            optionsBuilder.Options);
    }
}