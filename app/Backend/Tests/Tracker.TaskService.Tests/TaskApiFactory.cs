using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracker.TaskService.Data;

namespace Tracker.TaskService.Tests;

public sealed class TaskApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"task-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TaskDb"] = "Server=unused;Database=unused;User=unused;Password=unused;",
                ["AllowedFrontendOrigins"] = "http://localhost:4200",
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextOptions = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TaskDbContext>));
            if (dbContextOptions is not null)
            {
                services.Remove(dbContextOptions);
            }

            services.AddDbContext<TaskDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddHttpClient("AuthServiceVerify")
                .ConfigurePrimaryHttpMessageHandler(() => new FakeAuthServiceVerifyHandler());
        });
    }

    public HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string username, params string[] roles)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", $"Bearer {TestTokens.For(username, roles)}");
        return request;
    }
}
