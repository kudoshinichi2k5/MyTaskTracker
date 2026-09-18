using Microsoft.EntityFrameworkCore;
using Tracker.TaskService.Auth;
using Tracker.TaskService.Data;
using Tracker.TaskService.Endpoints;
using Tracker.TaskService.Services;

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
    .AddScheme<
        OpaqueTokenAuthOptions,
        OpaqueTokenAuthenticationHandler>(
        OpaqueTokenAuthOptions.SchemeName,
        options =>
        {
            options.AuthServiceBaseUrl = authServiceBaseUrl;
            options.VerifyPath = verifyPath;
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AdminOnly",
        policy => policy.RequireRole("admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
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

var taskDbConnectionString =
    builder.Configuration.GetConnectionString("TaskDb");

if (string.IsNullOrWhiteSpace(taskDbConnectionString))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:TaskDb. Set it via dotnet user-secrets (dev) " +
        "or the ConnectionStrings__TaskDb environment variable (staging/production).");
}

// builder.Services.AddDbContext<TaskDbContext>(options =>
//     options.UseMySql(
//         taskDbConnectionString,
//         ServerVersion.AutoDetect(taskDbConnectionString)));

builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseMySql(
        taskDbConnectionString,
        new MariaDbServerVersion(new Version(11, 4, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<ITaskStore, EfTaskStore>();


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
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        db.Database.EnsureCreated();
    }
}

// 2. KHỐI LOGIC MỚI: Chỉ dành riêng cho Helm Hook Migration (Chạy bằng lệnh: dotnet dll --migrate)
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    // NHỚ SỬA TÊN DbContext CHO ĐÚNG TỪNG SERVICE:
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    
    Console.WriteLine("Starting Database Migration for TaskService...");
    db.Database.Migrate();
    
    Console.WriteLine("Migration completed successfully.");
    return; // Thoát app ngay, không chạy Web Server (không chạy app.Run())
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

app.MapTaskEndpoints();

app.Run();

public partial class Program { }