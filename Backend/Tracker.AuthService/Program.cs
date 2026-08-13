using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Tracker.AuthService.Models;
using Tracker.AuthService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConcurrentDictionary<string, TokenInfo>>();
builder.Services.AddSingleton<ConcurrentDictionary<string, RefreshSession>>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, InternalAuthHandler>("Bearer", null);
builder.Services.AddAuthorization(options =>
{
    // Protects this service's own /api/v1/auth/admin/* endpoints below -
    // same policy name/shape Tracker.TaskService uses for its report
    // endpoint, so "admin" means the same thing everywhere.
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

var tokenStore = app.Services.GetRequiredService<ConcurrentDictionary<string, TokenInfo>>();
var refreshStore = app.Services.GetRequiredService<ConcurrentDictionary<string, RefreshSession>>();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// --- AUTH ENDPOINTS ---

app.MapPost("/token", async (HttpContext context, IUserStore users, IPasswordHasher<User> hasher) =>
{
    if (!context.Request.HasFormContentType)
        return Results.BadRequest(new { error = "invalid_request" });

    var form = await context.Request.ReadFormAsync();
    var grantType = form["grant_type"].ToString();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (!string.Equals(grantType, "password", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "unsupported_grant_type" });

    var issueResult = IssueToken(username, password, users, hasher, tokenStore, refreshStore);
    return issueResult is null
        ? Results.BadRequest(new { error = "invalid_grant" })
        : Results.Ok(issueResult);
});

app.MapPost("/api/v1/auth/login", (LoginRequest request, IUserStore users, IPasswordHasher<User> hasher) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "Username and password are required." });

    var result = IssueToken(request.Username, request.Password, users, hasher, tokenStore, refreshStore);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.MapPost("/api/v1/auth/register", (RegisterRequest request, IUserStore users, IPasswordHasher<User> hasher) =>
{
    var username = request.Username?.Trim() ?? string.Empty;
    var email = request.Email?.Trim() ?? string.Empty;

    if (username.Length < 3)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["username"] = ["Username must be at least 3 characters."] });
    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["password"] = ["Password must be at least 6 characters."] });
    if (users.UsernameExists(username))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["username"] = ["That username is already taken."] });

    var user = new User { Username = username, Email = email, PasswordHash = "" };
    user.PasswordHash = hasher.HashPassword(user, request.Password);
    // New accounts start with no roles - "admin" is granted explicitly from
    // the Admin Portal's Users screen, never at signup.
    users.Create(user);

    var result = IssueToken(username, request.Password, users, hasher, tokenStore, refreshStore);
    return Results.Ok(result);
});

app.MapPost("/api/v1/auth/refresh", (RefreshRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken)
        || !refreshStore.TryRemove(request.RefreshToken, out var session) // one-time use: rotation
        || session.ExpiresAt < DateTime.UtcNow)
    {
        return Results.Unauthorized();
    }

    var accessToken = Guid.NewGuid().ToString("N");
    var newRefreshToken = Guid.NewGuid().ToString("N");
    var expiresIn = 3600;

    tokenStore[accessToken] = new TokenInfo
    {
        Username = session.Username,
        Roles = session.Roles,
        RefreshToken = newRefreshToken,
        Expiry = DateTime.UtcNow.AddSeconds(expiresIn)
    };
    refreshStore[newRefreshToken] = session with { ExpiresAt = DateTime.UtcNow.AddDays(7) };

    return Results.Ok(new
    {
        accessToken,
        refreshToken = newRefreshToken,
        expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o"),
        username = session.Username,
        roles = session.Roles
    });
});

app.MapPost("/api/v1/auth/logout", (LogoutRequest request) =>
{
    if (!string.IsNullOrWhiteSpace(request.RefreshToken)
        && refreshStore.TryRemove(request.RefreshToken, out _))
    {
        foreach (var token in tokenStore.Where(pair => pair.Value.RefreshToken == request.RefreshToken))
        {
            tokenStore.TryRemove(token.Key, out _);
        }
    }

    return Results.Ok(new { success = true });
});

// Called by Tracker.TaskService / Tracker.NotificationService to validate a
// bearer token. Token travels in the Authorization header, NOT a query
// string - IIS (and most reverse proxies) log full request URLs including
// query strings by default, which would have written every access token
// used anywhere in the system into this service's own access logs.
app.MapGet("/verify", (HttpContext context) =>
{
    var authorization = context.Request.Headers.Authorization.ToString();
    if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Ok(new { active = false, username = (string?)null, roles = Array.Empty<string>() });
    }

    var token = authorization["Bearer ".Length..].Trim();
    if (tokenStore.TryGetValue(token, out var info))
    {
        if (info.Expiry > DateTime.UtcNow)
            return Results.Ok(new { active = true, username = info.Username, roles = info.Roles });

        tokenStore.TryRemove(token, out _);
    }
    return Results.Ok(new { active = false, username = (string?)null, roles = Array.Empty<string>() });
});

// --- ADMIN ENDPOINTS (Admin Portal's Users/Roles screens) ---
// Previously lived in Tracker.TaskService as a hardcoded SeedUsers list
// completely disconnected from real login - toggling a role there changed
// nothing about who could actually authenticate as what. Moved here so
// admin actions operate on the SAME store /token and /login read from.

var adminGroup = app.MapGroup("/api/v1/auth/admin").RequireAuthorization("AdminOnly");

adminGroup.MapGet("/users", IResult (IUserStore users) =>
    Results.Ok(users.GetAll().Select(u => new AdminUserSummary(u.Id, u.Username, u.Email, u.Enabled, u.Roles)).ToList()));

adminGroup.MapGet("/roles", IResult () => Results.Ok(new[] { "admin" }));

adminGroup.MapPost("/users/{userId}/roles/{role}", IResult (string userId, string role, IUserStore users) =>
{
    if (!users.AddRole(userId, role)) return Results.NotFound();
    // Existing access tokens keep whatever roles they already had baked in
    // until they naturally expire (1h) - revoking refresh tokens here just
    // stops that from being silently extended past this change.
    RevokeRefreshTokensForUser(userId, users, tokenStore, refreshStore);
    return Results.NoContent();
});

adminGroup.MapDelete("/users/{userId}/roles/{role}", IResult (string userId, string role, IUserStore users) =>
{
    if (!users.RemoveRole(userId, role)) return Results.NotFound();
    RevokeRefreshTokensForUser(userId, users, tokenStore, refreshStore);
    return Results.NoContent();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// --- HELPERS AND MODELS ---

static OAuthTokenResponse? IssueToken(
    string username, string password,
    IUserStore users, IPasswordHasher<User> hasher,
    ConcurrentDictionary<string, TokenInfo> tokenStore,
    ConcurrentDictionary<string, RefreshSession> refreshStore)
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;

    var user = users.FindByUsername(username.Trim());
    if (user is null || !user.Enabled) return null;

    var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
    if (verifyResult == PasswordVerificationResult.Failed) return null;

    var roles = user.Roles.ToArray();
    var accessToken = Guid.NewGuid().ToString("N");
    var refreshToken = Guid.NewGuid().ToString("N");
    var expiresIn = 3600;

    tokenStore[accessToken] = new TokenInfo
    {
        Username = user.Username,
        Roles = roles,
        RefreshToken = refreshToken,
        Expiry = DateTime.UtcNow.AddSeconds(expiresIn)
    };
    refreshStore[refreshToken] = new RefreshSession(user.Id, user.Username, roles, DateTime.UtcNow.AddDays(7));

    return new OAuthTokenResponse
    {
        AccessToken = accessToken, RefreshToken = refreshToken, TokenType = "bearer",
        ExpiresIn = expiresIn, Username = user.Username, Roles = roles,
        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o")
    };
}

static void RevokeRefreshTokensForUser(
    string userId, IUserStore users,
    ConcurrentDictionary<string, TokenInfo> tokenStore,
    ConcurrentDictionary<string, RefreshSession> refreshStore)
{
    var user = users.FindById(userId);
    if (user is null) return;

    foreach (var kvp in refreshStore.Where(kvp => kvp.Value.UserId == user.Id).ToList())
    {
        refreshStore.TryRemove(kvp.Key, out _);
    }
    foreach (var kvp in tokenStore.Where(kvp => string.Equals(kvp.Value.Username, user.Username, StringComparison.OrdinalIgnoreCase)).ToList())
    {
        tokenStore.TryRemove(kvp.Key, out _);
    }
}

// Handler xác thực nội bộ: Soi thẳng vào RAM (ConcurrentDictionary) thay vì gọi HTTP
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

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record RefreshSession(string UserId, string Username, string[] Roles, DateTime ExpiresAt);