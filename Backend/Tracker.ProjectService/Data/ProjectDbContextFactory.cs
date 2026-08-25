using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tracker.ProjectService.Data;

public sealed class ProjectDbContextFactory
    : IDesignTimeDbContextFactory<ProjectDbContext>
{
    public ProjectDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                "appsettings.json",
                optional: true)
            .AddJsonFile(
                "appsettings.Development.json",
                optional: true)
            .AddUserSecrets<ProjectDbContextFactory>(
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("ProjectDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:ProjectDb. " +
                "Configure it with dotnet user-secrets.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<ProjectDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            new MariaDbServerVersion(
                new Version(11, 4, 0)));

        return new ProjectDbContext(
            optionsBuilder.Options);
    }
}