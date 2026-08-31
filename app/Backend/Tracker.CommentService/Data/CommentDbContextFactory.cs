using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tracker.CommentService.Data;

public sealed class CommentDbContextFactory
    : IDesignTimeDbContextFactory<CommentDbContext>
{
    public CommentDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                "appsettings.json",
                optional: true)
            .AddJsonFile(
                "appsettings.Development.json",
                optional: true)
            .AddUserSecrets<CommentDbContextFactory>(
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("CommentDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:CommentDb. " +
                "Configure it with dotnet user-secrets.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<CommentDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            new MariaDbServerVersion(
                new Version(11, 4, 0)));

        return new CommentDbContext(
            optionsBuilder.Options);
    }
}