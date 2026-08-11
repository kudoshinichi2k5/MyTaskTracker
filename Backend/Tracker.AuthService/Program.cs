using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 1. Đưa các Store vào Dependency Injection (DI) để AuthHandler có thể dùng chung
builder.Services.AddSingleton<ConcurrentDictionary<string, TokenInfo>>();
builder.Services.AddSingleton<ConcurrentDictionary<string, RefreshSession>>();

// 2. Đăng ký Authentication sử dụng cơ chế kiểm tra nội bộ (Internal)
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, InternalAuthHandler>("Bearer", null);
builder.Services.AddAuthorization();

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

// Lấy các Store từ DI container ra để dùng cho Minimal APIs
var tokenStore = app.Services.GetRequiredService<ConcurrentDictionary<string, TokenInfo>>();
var refreshStore = app.Services.GetRequiredService<ConcurrentDictionary<string, RefreshSession>>();

// Bật Middlewares theo đúng thứ tự: CORS -> AuthN -> AuthZ
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// --- CÁC ENDPOINTS ---

app.MapPost("/token", async (HttpContext context) =>
{
    if (!context.Request.HasFormContentType)
        return Results.BadRequest(new { error = "invalid_request" });

    var form = await context.Request.ReadFormAsync();
    var grantType = form["grant_type"].ToString();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (!string.Equals(grantType, "password", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "unsupported_grant_type" });

    var issueResult = IssueToken(username, password, tokenStore, refreshStore);
    return issueResult is null
        ? Results.BadRequest(new { error = "invalid_grant" })
        : Results.Ok(issueResult);
});

app.MapPost("/api/v1/auth/login", (LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "Username and password are required." });

    var result = IssueToken(request.Username, request.Password, tokenStore, refreshStore);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.MapPost("/api/v1/auth/refresh", (RefreshRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken) || !refreshStore.TryGetValue(request.RefreshToken, out var session) || session.ExpiresAt < DateTime.UtcNow)
        return Results.Unauthorized();

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
        refreshStore.TryRemove(request.RefreshToken, out _);

    return Results.Ok(new { success = true });
});

// Endpoint này dành cho các Service khác (như Task, Notification) gọi sang để hỏi
app.MapGet("/verify", (string token) =>
{
    if (tokenStore.TryGetValue(token, out var info))
    {
        if (info.Expiry > DateTime.UtcNow)
            return Results.Ok(new { active = true, username = info.Username, roles = info.Roles });

        tokenStore.TryRemove(token, out _);
    }
    return Results.Ok(new { active = false, username = (string?)null, roles = Array.Empty<string>() });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Đọc key "Urls" (phòng hờ môi trường local)
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://localhost:5001");
app.Run();


// --- HÀM HỖ TRỢ VÀ MODEL ---

static OAuthTokenResponse? IssueToken(
    string username, string password,
    ConcurrentDictionary<string, TokenInfo> tokenStore,
    ConcurrentDictionary<string, RefreshSession> refreshStore)
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;
    if (!string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) || !string.Equals(password, "123456")) return null;

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
        AccessToken = accessToken, RefreshToken = refreshToken, TokenType = "bearer",
        ExpiresIn = expiresIn, Username = username, Roles = roles,
        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o")
    };
}

// Handler xác thực nội bộ: Soi thẳng vào RAM (ConcurrentDictionary) thay vì gọi HTTP
public sealed class InternalAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ConcurrentDictionary<string, TokenInfo> _tokenStore;

    public InternalAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ConcurrentDictionary<string, TokenInfo> tokenStore) : base(options, logger, encoder)
    {
        _tokenStore = tokenStore; // Lấy từ DI Container
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(AuthenticateResult.Fail("Bearer token is empty."));

        // Dò trong RAM, nhanh hơn rất nhiều so với gọi HTTP
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