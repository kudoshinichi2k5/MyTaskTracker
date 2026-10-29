using System.Security.Claims;
using System.Threading.RateLimiting;
using Prometheus;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tracker.AuthService.Data;
using Tracker.AuthService.Models;
using Tracker.AuthService.Services;
using Tracker.AuthService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false 
    };
});

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

if (!builder.Environment.IsEnvironment("Testing"))
{
    var valkeyConnectionString = builder.Configuration.GetConnectionString("Valkey");
    if (string.IsNullOrWhiteSpace(valkeyConnectionString))
    {
        throw new InvalidOperationException(
            "Missing ConnectionStrings:Valkey. Set it via dotnet user-secrets (dev) " +
            "or the ConnectionStrings__Valkey environment variable (staging/production).");
    }

    var valkeyOptions = ConfigurationOptions.Parse(valkeyConnectionString);
    var valkeyPassword = builder.Configuration["Valkey:Password"]
        ?? builder.Configuration["VALKEY_PASSWORD"];

    if (!string.IsNullOrWhiteSpace(valkeyPassword))
    {
        valkeyOptions.Password = valkeyPassword;
    }

    valkeyOptions.AbortOnConnectFail = false;
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(valkeyOptions));
    builder.Services.AddSingleton<ITokenStore, ValkeyTokenStore>();
}

var authDbConnectionString =
    builder.Configuration.GetConnectionString("AuthDb");

if (string.IsNullOrWhiteSpace(authDbConnectionString))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:AuthDb. Set it via dotnet user-secrets (dev) " +
        "or the ConnectionStrings__AuthDb environment variable (staging/production).");
}

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(
        authDbConnectionString,
        new MariaDbServerVersion(new Version(11, 4, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IUserStore, EfUserStore>();
builder.Services.AddSingleton<OAuthClientRegistry>();

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, InternalAuthHandler>("Bearer", null);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var configuredOrigins = (builder.Configuration["AllowedFrontendOrigins"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/'));

        var localOrigins = new[] { "http://localhost:4200", "http://localhost:4300" };
        var allOrigins = configuredOrigins.Concat(localOrigins).Distinct().ToArray();

        policy.WithOrigins(allOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("oauth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpMetrics();

// 1. CHỈ BẬT SWAGGER CHO MÔI TRƯỜNG DEV/TESTING
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Dành cho Unit Test (Testing)
    if (app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        db.Database.EnsureCreated();
        // Vẫn gọi Seeder trong lúc test để có data test
        AuthDbSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
    }
}

// 2. KHỐI LOGIC MỚI: Chỉ dành cho Helm Hook (Chạy Migration và Seeding)
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    
    Console.WriteLine("Starting Database Migration for AuthService...");
    db.Database.Migrate();
    
    Console.WriteLine("Starting Database Seeding...");
    AuthDbSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
    
    Console.WriteLine("Migration and Seeding completed successfully.");
    return; // Thoát app ngay, không chạy Web Server
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapOAuthEndpoints();

app.MapMetrics();
app.Run();

public partial class Program { }

// --- AUTH HANDLER & SHARED STATE MODELS --- (giữ nguyên toàn bộ phần dưới, không đổi)

public sealed class InternalAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITokenStore _tokenStore;

    public InternalAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ITokenStore tokenStore) : base(options, logger, encoder)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.Fail("Bearer token is empty.");

        var info = await _tokenStore.GetAccessTokenAsync(token);
        if (info is not null && info.Expiry > DateTime.UtcNow)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, info.Username),
                new(ClaimTypes.NameIdentifier, info.Username)
            };
            claims.AddRange(info.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
        }

        if (info is not null)
        {
            await _tokenStore.RevokeAccessTokenAsync(token);
        }

        return AuthenticateResult.Fail("Token is invalid or expired.");
    }
}

public class TokenInfo
{
    public string Username { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }
}

public record RefreshSession(string UserId, string Username, string[] Roles, DateTime ExpiresAt);

public record AuthorizationRequest(
    string ClientId,
    string RedirectUri,
    string Scope,
    string State,
    string CodeChallenge,
    DateTime ExpiresAt);

public record AuthorizationCode(
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string UserId,
    string Username,
    string[] Roles,
    DateTime ExpiresAt);

public class OAuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "bearer";
    public int ExpiresIn { get; set; }
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string ExpiresAt { get; set; } = string.Empty;
}
