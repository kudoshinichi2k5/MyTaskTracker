using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tracker.AuthService.Data;

public class AuthDbContextFactory
    : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile(
                "appsettings.Development.json",
                optional: true)
            .AddUserSecrets<AuthDbContextFactory>(
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("AuthDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:AuthDb. " +
                "Configure it with dotnet user-secrets.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<AuthDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            new MariaDbServerVersion(
                new Version(11, 4, 0)));

        return new AuthDbContext(optionsBuilder.Options);
    }
}