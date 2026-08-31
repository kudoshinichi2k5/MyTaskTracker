using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tracker.NotificationService.Data;

public sealed class NotificationDbContextFactory
    : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                "appsettings.json",
                optional: true)
            .AddJsonFile(
                "appsettings.Development.json",
                optional: true)
            .AddUserSecrets<NotificationDbContextFactory>(
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString(
                "NotificationDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:NotificationDb. " +
                "Configure it with dotnet user-secrets.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<NotificationDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            new MariaDbServerVersion(
                new Version(11, 4, 0)));

        return new NotificationDbContext(
            optionsBuilder.Options);
    }
}