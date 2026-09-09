using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracker.NotificationService.Data;

namespace Tracker.NotificationService.Tests;

public sealed class NotificationApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"notification-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotificationDb"] = "Server=unused;Database=unused;User=unused;Password=unused;",
            ["AllowedFrontendOrigins"] = "http://localhost:4200",
        }));
        builder.ConfigureServices(services =>
        {
            var registration = services.Single(d => d.ServiceType == typeof(DbContextOptions<NotificationDbContext>));
            services.Remove(registration);
            services.AddDbContext<NotificationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddHttpClient("AuthServiceVerify").ConfigurePrimaryHttpMessageHandler(() => new FakeAuthServiceVerifyHandler());
        });
    }
}
