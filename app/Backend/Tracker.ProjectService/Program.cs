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

// 1. CHỈ GIỮ LẠI Swagger cho môi trường Dev/Testing (Không Production)
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // (Bổ sung cho Unit Test): Unit test không có khái niệm chạy job riêng, nó tự setup DB bộ nhớ lúc khởi động.
    if (app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Database.EnsureCreated();
    }
}

// 2. KHỐI LOGIC MỚI: Chỉ dành riêng cho Helm Hook Migration (Chạy bằng lệnh: dotnet dll --migrate)
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    // NHỚ SỬA TÊN DbContext CHO ĐÚNG TỪNG SERVICE:
    var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    
    Console.WriteLine("Starting Database Migration for ProjectService...");
    db.Database.Migrate();
    
    Console.WriteLine("Migration completed successfully.");
    return; // Thoát app ngay, không chạy Web Server (không chạy app.Run())
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

public partial class Program { }