git add Frontend/AdminPortal
git commit -m "fix(frontend): complete admin JWT login flow"using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"] ?? "http://localhost:5001";
var audience = jwtSection["Audience"] ?? "task-tracker-clients";
var secretKey = jwtSection["SecretKey"] ?? "SuperSecretKeyForTaskTrackerSystemDoNotShare2026!";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = signingKey,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var refreshStore = new ConcurrentDictionary<string, RefreshSession>();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", environment = app.Environment.EnvironmentName }));

app.MapPost("/token", async (HttpContext http) =>
{
    var request = await TryReadTokenRequestAsync(http.Request);
    if (request is null)
    {
        return Results.BadRequest(new { error = "invalid_request" });
    }

    if (string.Equals(request.grant_type, "password", StringComparison.OrdinalIgnoreCase))
    {
        var username = request.username ?? string.Empty;
        var password = request.password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Results.BadRequest(new { error = "invalid_grant" });
        }

        var roles = username.Equals("admin", StringComparison.OrdinalIgnoreCase)
            ? new[] { "admin", "user" }
            : new[] { "user" };

        var accessToken = CreateJwtToken(username, roles, issuer, audience, secretKey);
        var refreshToken = Guid.NewGuid().ToString("N");
        refreshStore[refreshToken] = new RefreshSession(username, roles, DateTime.UtcNow.AddDays(7));

        return Results.Ok(new
        {
            access_token = accessToken,
            refresh_token = refreshToken,
            token_type = "bearer",
            expires_in = 3600,
            username,
            roles
        });
    }

    if (string.Equals(request.grant_type, "refresh_token", StringComparison.OrdinalIgnoreCase))
    {
        var refreshToken = request.refresh_token ?? string.Empty;
        if (!refreshStore.TryGetValue(refreshToken, out var session) || session.ExpiresAt < DateTime.UtcNow)
        {
            return Results.BadRequest(new { error = "invalid_grant" });
        }

        var accessToken = CreateJwtToken(session.Username, session.Roles, issuer, audience, secretKey);
        return Results.Ok(new
        {
            access_token = accessToken,
            refresh_token = refreshToken,
            token_type = "bearer",
            expires_in = 3600,
            username = session.Username,
            roles = session.Roles
        });
    }

    return Results.BadRequest(new { error = "unsupported_grant_type" });
});

app.MapPost("/api/v1/auth/login", (LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    var username = request.Username.Trim();
    var roles = username.Equals("admin", StringComparison.OrdinalIgnoreCase)
        ? new[] { "admin", "user" }
        : new[] { "user" };

    var accessToken = CreateJwtToken(username, roles, issuer, audience, secretKey);
    var refreshToken = Guid.NewGuid().ToString("N");
    refreshStore[refreshToken] = new RefreshSession(username, roles, DateTime.UtcNow.AddDays(7));

    return Results.Ok(new
    {
        accessToken,
        refreshToken,
        expiresAt = DateTimeOffset.UtcNow.AddHours(1).ToString("o"),
        username,
        roles
    });
});

app.MapPost("/api/v1/auth/refresh", (RefreshRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken) || !refreshStore.TryGetValue(request.RefreshToken, out var session) || session.ExpiresAt < DateTime.UtcNow)
    {
        return Results.Unauthorized();
    }

    var accessToken = CreateJwtToken(session.Username, session.Roles, issuer, audience, secretKey);
    return Results.Ok(new
    {
        accessToken,
        refreshToken = request.RefreshToken,
        expiresAt = DateTimeOffset.UtcNow.AddHours(1).ToString("o"),
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

app.MapGet("/api/v1/auth/me", [Authorize]() => Results.Ok(new { status = "ok" }));

app.Run();

static async Task<TokenRequest?> TryReadTokenRequestAsync(HttpRequest request)
{
    if (request.HasFormContentType)
    {
        var form = await request.ReadFormAsync();
        return new TokenRequest(
            form["grant_type"].ToString(),
            form["username"].ToString(),
            form["password"].ToString(),
            form["refresh_token"].ToString());
    }

    if (request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
    {
        try
        {
            var body = await request.ReadFromJsonAsync<TokenRequest>();
            return body;
        }
        catch
        {
            return null;
        }
    }

    return null;
}

static string CreateJwtToken(string username, IEnumerable<string> roles, string issuer, string audience, string secretKey)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, username),
        new(JwtRegisteredClaimNames.UniqueName, username),
        new(ClaimTypes.Name, username)
    };

    foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: cred);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

record LoginRequest(string Username, string Password);
record RefreshRequest(string RefreshToken);
record LogoutRequest(string RefreshToken);
record TokenRequest(string? grant_type, string? username, string? password, string? refresh_token);
record RefreshSession(string Username, string[] Roles, DateTime ExpiresAt);
