using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AuthService.Data;

namespace Tracker.AuthService.Tests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"auth-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] = "Server=unused;Database=unused;User=unused;Password=unused;",
                ["AllowedFrontendOrigins"] = "http://localhost:4200",
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextOptions = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
            if (dbContextOptions is not null)
            {
                services.Remove(dbContextOptions);
            }

            services.AddDbContext<AuthDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
