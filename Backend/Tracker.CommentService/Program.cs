using Microsoft.EntityFrameworkCore;
using Tracker.CommentService.Auth;
using Tracker.CommentService.Data;
using Tracker.CommentService.Endpoints;
using Tracker.CommentService.Services;

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

var commentDbConnectionString =
    builder.Configuration.GetConnectionString("CommentDb");

if (string.IsNullOrWhiteSpace(commentDbConnectionString))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:CommentDb. Set it via dotnet user-secrets (dev) " +
        "or the ConnectionStrings__CommentDb environment variable (staging/production).");
}

builder.Services.AddDbContext<CommentDbContext>(options =>
    options.UseMySql(
        commentDbConnectionString,
        ServerVersion.AutoDetect(commentDbConnectionString)));

builder.Services.AddScoped<CommentStore>();

// Only known frontend origins may send credentialed requests. The previous
// SetIsOriginAllowed(_ => true) + AllowCredentials() combination accepted
// credentialed requests from *any* origin - fixed to match the allow-list
// pattern already used by AuthService/TaskService/NotificationService.
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
            "http://localhost:4300"
        };

        policy
            .WithOrigins(configuredOrigins.Concat(localOrigins).Distinct().ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CommentDbContext>().Database.Migrate();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    "/health",
    () => Results.Ok(new { status = "healthy", service = "Tracker.CommentService" }))
    .AllowAnonymous();

app.MapCommentEndpoints();

app.Run();