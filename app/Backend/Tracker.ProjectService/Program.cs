using Microsoft.EntityFrameworkCore;

using Tracker.ProjectService.Auth;
using Tracker.ProjectService.Data;
using Tracker.ProjectService.Endpoints;
using Tracker.ProjectService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("AuthServiceVerify");

builder.Services
    .AddAuthentication(OpaqueTokenAuthOptions.SchemeName)
    .AddScheme<OpaqueTokenAuthOptions, OpaqueTokenAuthenticationHandler>(
        OpaqueTokenAuthOptions.SchemeName,
        options =>
        {
            options.AuthServiceBaseUrl =
                builder.Configuration["AuthService:BaseUrl"]
                ?? "http://localhost:5001";

            options.VerifyPath =
                builder.Configuration["AuthService:VerifyPath"]
                ?? "/verify";
        });

builder.Services.AddAuthorization();

var projectDbConnectionString =
    builder.Configuration.GetConnectionString("ProjectDb");

if (string.IsNullOrWhiteSpace(projectDbConnectionString))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:ProjectDb. Set it via dotnet user-secrets (dev) " +
        "or the ConnectionStrings__ProjectDb environment variable (staging/production).");
}

builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseMySql(
        projectDbConnectionString,
        new MariaDbServerVersion(new Version(11, 4, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<ProjectStore>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var configuredOrigins =
            (builder.Configuration["AllowedFrontendOrigins"] ?? "")
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(origin => origin.TrimEnd('/'));

        var localOrigins = new[]
        {
            "http://localhost:4200",
            "http://localhost:4300"
        };

        policy
            .WithOrigins(
                configuredOrigins
                    .Concat(localOrigins)
                    .Distinct()
                    .ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();

    scope
        .ServiceProvider
        .GetRequiredService<ProjectDbContext>()
        .Database
        .Migrate();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "healthy",
        service = "Tracker.ProjectService"
    }))
    .AllowAnonymous();

app.MapProjectEndpoints();

app.Run();