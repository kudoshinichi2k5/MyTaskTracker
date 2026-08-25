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
    builder.Configuration.GetConnectionString("ProjectDb")
    ?? throw new InvalidOperationException(
        "Missing ConnectionStrings:ProjectDb. Set it via dotnet user-secrets (dev) " +
        "or the ConnectionStrings__ProjectDb environment variable (staging/production).");

builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseMySql(
        projectDbConnectionString,
        ServerVersion.AutoDetect(projectDbConnectionString)));

builder.Services.AddScoped<ProjectStore>();

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
    scope.ServiceProvider.GetRequiredService<ProjectDbContext>().Database.Migrate();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    "/health",
    () => Results.Ok(new { status = "healthy", service = "Tracker.ProjectService" }))
    .AllowAnonymous();

app.MapProjectEndpoints();

app.Run();