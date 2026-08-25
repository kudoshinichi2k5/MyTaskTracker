using Microsoft.EntityFrameworkCore;
using Tracker.NotificationService.Auth;
using Tracker.NotificationService.Data;
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
        client.BaseAddress = new Uri(authServiceBaseUrl);
    });

builder.Services
    .AddAuthentication(OpaqueTokenAuthOptions.SchemeName)
    .AddScheme<OpaqueTokenAuthOptions, OpaqueTokenAuthenticationHandler>(
        OpaqueTokenAuthOptions.SchemeName,
        options =>
        {
            options.AuthServiceBaseUrl = authServiceBaseUrl;
            options.VerifyPath = verifyPath;
        });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var configuredOrigins =
            (builder.Configuration["AllowedFrontendOrigins"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(origin => origin.TrimEnd('/'));

        var localOrigins = new[]
        {
            "http://localhost:4200",
            "http://localhost:4300",
            "http://app.testing.local"
        };

        policy
            .WithOrigins(configuredOrigins.Concat(localOrigins).Distinct().ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var notificationDbConnectionString =
    builder.Configuration.GetConnectionString("NotificationDb");

if (string.IsNullOrWhiteSpace(notificationDbConnectionString))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:NotificationDb. Set it via dotnet user-secrets (dev) " +
        "or the ConnectionStrings__NotificationDb environment variable (staging/production).");
}

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseMySql(
        notificationDbConnectionString,
        ServerVersion.AutoDetect(notificationDbConnectionString)));

builder.Services.AddScoped<INotificationStore, EfNotificationStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.Migrate();
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
            environment = app.Environment.EnvironmentName
        }));

app.MapNotificationEndpoints();

app.Run();