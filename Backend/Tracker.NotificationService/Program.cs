using Tracker.NotificationService.Auth;
using Tracker.NotificationService.Endpoints;
using Tracker.NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

var authServiceBaseUrl =
    builder.Configuration["AuthService:BaseUrl"]
    ?? "http://localhost:5001";

var verifyPath =
    builder.Configuration["AuthService:VerifyPath"]
    ?? "verify";

builder.Services.AddHttpClient(
    "AuthServiceVerify",
    client =>
    {
        client.BaseAddress =
            new Uri(authServiceBaseUrl);
    });

builder.Services
    .AddAuthentication(
        OpaqueTokenAuthOptions.SchemeName)
    .AddScheme<
        OpaqueTokenAuthOptions,
        OpaqueTokenAuthenticationHandler>(
        OpaqueTokenAuthOptions.SchemeName,
        options =>
        {
            options.AuthServiceBaseUrl =
                authServiceBaseUrl;

            options.VerifyPath =
                verifyPath;
        });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            var configuredOrigins =
                (builder.Configuration[
                    "AllowedFrontendOrigins"] ?? "")
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Select(origin => origin.TrimEnd('/'));

            var localOrigins = new[]
            {
                "http://localhost:4200",
                "http://localhost:4300",
                "http://app.testing.local"
            };

            policy
                .WithOrigins(
                    configuredOrigins
                        .Concat(localOrigins)
                        .Distinct()
                        .ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddSingleton<
    INotificationStore,
    InMemoryNotificationStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            status = "healthy",
            environment =
                app.Environment.EnvironmentName
        }));

app.MapNotificationEndpoints();

app.Run();