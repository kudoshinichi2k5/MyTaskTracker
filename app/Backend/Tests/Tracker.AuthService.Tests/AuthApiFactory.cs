using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;
using Tracker.AuthService.Data;
using Tracker.AuthService.Services;

namespace Tracker.AuthService.Tests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"auth-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] = "Server=unused;Database=unused;User=unused;Password=unused;",
                ["ConnectionStrings:Valkey"] = "localhost:6379,password=not-used",
                ["AllowedFrontendOrigins"] = "http://localhost:4200",
                ["OAuth:Clients:mytasktracker-customer:RedirectUris:0"] = "http://localhost:4200/oauth/callback",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var dbContextOptions = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
            if (dbContextOptions is not null)
            {
                services.Remove(dbContextOptions);
            }

            services.AddDbContext<AuthDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<ITokenStore>();
            services.AddSingleton<ITokenStore, InMemoryTokenStore>();
        });
    }
}
