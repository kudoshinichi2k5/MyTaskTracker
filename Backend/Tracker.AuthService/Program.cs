using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var tokenStore = new ConcurrentDictionary<string, TokenInfo>();

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

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return Results.BadRequest(new { error = "invalid_grant" });
    }

    if (!string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) || !string.Equals(password, "123456"))
    {
        return Results.BadRequest(new { error = "invalid_grant" });
    }

    var accessToken = Guid.NewGuid().ToString("N");
    var expiresIn = 3600;

    tokenStore[accessToken] = new TokenInfo
    {
        Username = username,
        Expiry = DateTime.UtcNow.AddSeconds(expiresIn)
    };

    return Results.Ok(new
    {
        access_token = accessToken,
        token_type = "bearer",
        expires_in = expiresIn
    });
});

app.MapGet("/verify", (string token) =>
{
    if (tokenStore.TryGetValue(token, out var info))
    {
        if (info.Expiry > DateTime.UtcNow)
        {
            return Results.Ok(new { active = true, username = info.Username });
        }

        tokenStore.TryRemove(token, out _);
    }

    return Results.Ok(new { active = false });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run("http://localhost:5000");

public class TokenInfo
{
    public string Username { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }
}
