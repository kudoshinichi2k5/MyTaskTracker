using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = (builder.Configuration["AllowedFrontendOrigins"]
                        ?? "http://localhost:4200,http://localhost:4300")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
var tokenStore = new ConcurrentDictionary<string, TokenInfo>();
var refreshStore = new ConcurrentDictionary<string, RefreshSession>();

app.UseCors("AllowFrontend");

app.MapPost("/token", async (HttpContext context) =>
{
    if (!context.Request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "invalid_request" });
    }

    var form = await context.Request.ReadFormAsync();
    var grantType = form["grant_type"].ToString();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (!string.Equals(grantType, "password", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "unsupported_grant_type" });
    }

    var issueResult = IssueToken(username, password, tokenStore, refreshStore);
    return issueResult is null
        ? Results.BadRequest(new { error = "invalid_grant" })
        : Results.Ok(issueResult);
});

app.MapPost("/api/v1/auth/login", (LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    var result = IssueToken(request.Username, request.Password, tokenStore, refreshStore);
    return result is null
        ? Results.Unauthorized()
        : Results.Ok(result);
});

app.MapPost("/api/v1/auth/refresh", (RefreshRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken) || !refreshStore.TryGetValue(request.RefreshToken, out var session) || session.ExpiresAt < DateTime.UtcNow)
    {
        return Results.Unauthorized();
    }

    var accessToken = Guid.NewGuid().ToString("N");
    var expiresIn = 3600;
    tokenStore[accessToken] = new TokenInfo
    {
        Username = session.Username,
        Roles = session.Roles,
        Expiry = DateTime.UtcNow.AddSeconds(expiresIn)
    };

    return Results.Ok(new
    {
        accessToken,
        refreshToken = request.RefreshToken,
        expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o"),
        username = session.Username,
        roles = session.Roles
    });
});

app.MapPost("/api/v1/auth/logout", (LogoutRequest request) =>
{
    if (!string.IsNullOrWhiteSpace(request.RefreshToken))
    {
        refreshStore.TryRemove(request.RefreshToken, out _);
    }

    return Results.Ok(new { success = true });
});

app.MapGet("/verify", (string token) =>
{
    if (tokenStore.TryGetValue(token, out var info))
    {
        if (info.Expiry > DateTime.UtcNow)
        {
            return Results.Ok(new { active = true, username = info.Username, roles = info.Roles });
        }

        tokenStore.TryRemove(token, out _);
    }

    return Results.Ok(new { active = false, username = (string?)null, roles = Array.Empty<string>() });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Respects the "Urls" key in appsettings.{Environment}.json / ASPNETCORE_URLS
// so each environment (and the IIS in-process host) can bind independently
// instead of every deployment being hardcoded to localhost:5001.
app.Run();

static OAuthTokenResponse? IssueToken(
    string username,
    string password,
    ConcurrentDictionary<string, TokenInfo> tokenStore,
    ConcurrentDictionary<string, RefreshSession> refreshStore)
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return null;
    }

    if (!string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) || !string.Equals(password, "123456"))
    {
        return null;
    }

    // Demo credential store: only "admin" can sign in today, and always gets
    // both roles. Swap this block for a real user/role lookup before this
    // goes anywhere near production.
    var roles = new[] { "admin", "user" };
    var accessToken = Guid.NewGuid().ToString("N");
    var refreshToken = Guid.NewGuid().ToString("N");
    var expiresIn = 3600;

    tokenStore[accessToken] = new TokenInfo
    {
        Username = username,
        Roles = roles,
        Expiry = DateTime.UtcNow.AddSeconds(expiresIn)
    };

    refreshStore[refreshToken] = new RefreshSession(username, roles, DateTime.UtcNow.AddDays(7));

    return new OAuthTokenResponse
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        TokenType = "bearer",
        ExpiresIn = expiresIn,
        Username = username,
        Roles = roles,
        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o")
    };
}

public class TokenInfo
{
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
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
public record RefreshSession(string Username, string[] Roles, DateTime ExpiresAt);