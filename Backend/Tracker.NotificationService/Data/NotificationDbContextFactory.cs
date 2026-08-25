using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tracker.NotificationService.Data;

public class NotificationDbContextFactory
    : IDesignTimeDbContextFactory<
        NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(
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
                    NotificationDbContextFactory>(
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

        var connectionString =
            configuration.GetConnectionString("NotificationDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost;Port=3306;Database=tracker_notifications;User=notification_service;Password=change_me_notification;";
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<
                NotificationDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(
                connectionString));

        return new NotificationDbContext(
            optionsBuilder.Options);
    }
}
