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

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true)
    .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CommentDbContext>().Database.Migrate();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    "/health",
    () => Results.Ok(new { status = "healthy", service = "Tracker.CommentService" }))
    .AllowAnonymous();

app.MapCommentEndpoints();

app.Run();