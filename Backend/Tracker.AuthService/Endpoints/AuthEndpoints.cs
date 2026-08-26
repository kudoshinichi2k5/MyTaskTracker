using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Tracker.AuthService.Models;
using Tracker.AuthService.Services;

namespace Tracker.AuthService.Endpoints;

public static class AuthEndpoints
{
    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Lấy các Stores từ Dependency Injection để dùng chung cho các endpoints
        var tokenStore = app.ServiceProvider.GetRequiredService<ConcurrentDictionary<string, TokenInfo>>();
        var refreshStore = app.ServiceProvider.GetRequiredService<ConcurrentDictionary<string, RefreshSession>>();

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
            if (string.IsNullOrWhiteSpace(email) || !EmailPattern.IsMatch(email))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["A valid email address is required."] });
            if (users.UsernameExists(username))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["username"] = ["That username is already taken."] });
            if (users.EmailExists(email))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["That email is already registered."] });

            var user = new User { Username = username, Email = email, PasswordHash = "" };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            users.Create(user);

            var result = IssueToken(username, request.Password, users, hasher, tokenStore, refreshStore);
            return Results.Ok(result);
        });

        app.MapPost("/api/v1/auth/refresh", (RefreshRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken)
                || !refreshStore.TryRemove(request.RefreshToken, out var session)
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

        var adminGroup = app.MapGroup("/api/v1/auth/admin").RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/users", IResult (IUserStore users) =>
            Results.Ok(users.GetAll().Select(u => new AdminUserSummary(u.Id, u.Username, u.Email, u.Enabled, u.Roles)).ToList()));

        adminGroup.MapGet("/roles", IResult () => Results.Ok(new[] { "admin" }));

        adminGroup.MapPost("/users/{userId}/roles/{role}", IResult (string userId, string role, IUserStore users) =>
        {
            if (!users.AddRole(userId, role)) return Results.NotFound();
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
    }

    // --- HELPERS BÊN TRONG ENDPOINTS ---

    private static OAuthTokenResponse? IssueToken(
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

    private static void RevokeRefreshTokensForUser(
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
}

// --- DTOs DÀNH RIÊNG CHO ENDPOINTS ---
public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);