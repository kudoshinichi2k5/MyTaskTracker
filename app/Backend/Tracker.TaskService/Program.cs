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

// Áp migration tự động khi KHÔNG phải Production — Production chạy migration
// tường minh qua CI/CD (mục 8), không để app tự ALTER TABLE lúc khởi động.
if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<TaskDbContext>().Database.Migrate();
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