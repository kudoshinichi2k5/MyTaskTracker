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
            connectionString = "Server=localhost;Port=3306;Database=tracker_auth;User=auth_service;Password=change_me_auth;";
        }

        Console.WriteLine($"[DEBUG] connectionString = '{connectionString}'");

        var optionsBuilder =
            new DbContextOptionsBuilder<AuthDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString));

        return new AuthDbContext(optionsBuilder.Options);
    }
}