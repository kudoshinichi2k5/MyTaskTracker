using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tracker.TaskService.Data;

public sealed class TaskDbContextFactory
    : IDesignTimeDbContextFactory<TaskDbContext>
{
    public TaskDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                "appsettings.json",
                optional: true)
            .AddJsonFile(
                "appsettings.Development.json",
                optional: true)
            .AddUserSecrets<TaskDbContextFactory>(
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("TaskDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:TaskDb. " +
                "Configure it with dotnet user-secrets.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<TaskDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            new MariaDbServerVersion(
                new Version(11, 4, 0)));

        return new TaskDbContext(
            optionsBuilder.Options);
    }
}