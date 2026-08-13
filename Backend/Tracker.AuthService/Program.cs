using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Tracker.AuthService.Models;
using Tracker.AuthService.Services;
using Tracker.AuthService.Endpoints; // Import namespace mới

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConcurrentDictionary<string, TokenInfo>>();
builder.Services.AddSingleton<ConcurrentDictionary<string, RefreshSession>>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, InternalAuthHandler>("Bearer", null);
builder.Services.AddAuthorization(options =>
{
    // Protects this service's own /api/v1/auth/admin/* endpoints
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

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// --- ĐĂNG KÝ ENDPOINTS TỪ KẾT CẤU BÊN NGOÀI ---
app.MapAuthEndpoints();

app.Run();

// --- AUTH HANDLER & SHARED STATE MODELS ---

public sealed class InternalAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ConcurrentDictionary<string, TokenInfo> _tokenStore;

    public InternalAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ConcurrentDictionary<string, TokenInfo> tokenStore) : base(options, logger, encoder)
    {
        _tokenStore = tokenStore;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(AuthenticateResult.Fail("Bearer token is empty."));

        if (_tokenStore.TryGetValue(token, out var info) && info.Expiry > DateTime.UtcNow)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, info.Username),
                new(ClaimTypes.NameIdentifier, info.Username)
            };
            claims.AddRange(info.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }

        return Task.FromResult(AuthenticateResult.Fail("Token is invalid or expired."));
    }
}

public class TokenInfo
{
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }
}

public record RefreshSession(string UserId, string Username, string[] Roles, DateTime ExpiresAt);

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