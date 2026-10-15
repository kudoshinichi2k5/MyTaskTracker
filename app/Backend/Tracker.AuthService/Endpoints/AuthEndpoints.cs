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
        app.MapPost("/token", async Task<IResult> (HttpContext context, IUserStore users, IPasswordHasher<User> hasher, ITokenStore tokenStore) =>
        {
            if (!context.Request.HasFormContentType)
                return Results.BadRequest(new { error = "invalid_request" });

            var form = await context.Request.ReadFormAsync();
            if (!string.Equals(form["grant_type"].ToString(), "password", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "unsupported_grant_type" });

            var result = await IssueTokenAsync(form["username"].ToString(), form["password"].ToString(), users, hasher, tokenStore);
            return result is null ? Results.BadRequest(new { error = "invalid_grant" }) : Results.Ok(result);
        });

        app.MapPost("/api/v1/auth/login", async Task<IResult> (LoginRequest request, IUserStore users, IPasswordHasher<User> hasher, ITokenStore tokenStore) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Username and password are required." });

            var result = await IssueTokenAsync(request.Username, request.Password, users, hasher, tokenStore);
            return result is null ? Results.Unauthorized() : Results.Ok(result);
        });

        app.MapPost("/api/v1/auth/register", async Task<IResult> (RegisterRequest request, IUserStore users, IPasswordHasher<User> hasher, ITokenStore tokenStore) =>
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

            var user = new User { Username = username, Email = email, PasswordHash = string.Empty };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            users.Create(user);

            var result = await IssueTokenAsync(username, request.Password, users, hasher, tokenStore);
            return Results.Ok(result);
        });

        app.MapPost("/api/v1/auth/refresh", async Task<IResult> (RefreshRequest request, ITokenStore tokenStore) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Results.Unauthorized();

            var session = await tokenStore.ConsumeRefreshSessionAsync(request.RefreshToken);
            if (session is null || session.ExpiresAt < DateTime.UtcNow)
                return Results.Unauthorized();

            var accessToken = Guid.NewGuid().ToString("N");
            var refreshToken = Guid.NewGuid().ToString("N");
            var accessLifetime = TimeSpan.FromHours(1);
            var refreshLifetime = TimeSpan.FromDays(7);

            await tokenStore.StoreAccessTokenAsync(accessToken, new TokenInfo
            {
                Username = session.Username,
                Roles = session.Roles,
                RefreshTokenHash = TokenHashing.Compute(refreshToken),
                Expiry = DateTime.UtcNow.Add(accessLifetime)
            }, accessLifetime);
            await tokenStore.StoreRefreshSessionAsync(refreshToken, session with { ExpiresAt = DateTime.UtcNow.Add(refreshLifetime) }, refreshLifetime);

            return Results.Ok(new
            {
                accessToken,
                refreshToken,
                expiresAt = DateTimeOffset.UtcNow.Add(accessLifetime).ToString("o"),
                username = session.Username,
                roles = session.Roles
            });
        });

        app.MapPost("/api/v1/auth/logout", async (LogoutRequest request, ITokenStore tokenStore) =>
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var session = await tokenStore.GetRefreshSessionAsync(request.RefreshToken);
                if (session is not null)
                {
                    await tokenStore.RevokeRefreshSessionAsync(request.RefreshToken);
                    await tokenStore.RevokeAccessTokensForRefreshTokenAsync(session.Username, request.RefreshToken);
                }
            }

            return Results.Ok(new { success = true });
        });

        app.MapGet("/verify", async Task<IResult> (HttpContext context, ITokenStore tokenStore) =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Ok(new { active = false, username = (string?)null, roles = Array.Empty<string>() });

            var token = authorization["Bearer ".Length..].Trim();
            var info = await tokenStore.GetAccessTokenAsync(token);
            if (info is not null)
            {
                if (info.Expiry > DateTime.UtcNow)
                    return Results.Ok(new { active = true, username = info.Username, roles = info.Roles });

                await tokenStore.RevokeAccessTokenAsync(token);
            }

            return Results.Ok(new { active = false, username = (string?)null, roles = Array.Empty<string>() });
        });

        var adminGroup = app.MapGroup("/api/v1/auth/admin").RequireAuthorization("AdminOnly");
        adminGroup.MapGet("/users", IResult (IUserStore users) =>
            Results.Ok(users.GetAll().Select(u => new AdminUserSummary(u.Id, u.Username, u.Email, u.Enabled, u.Roles)).ToList()));
        adminGroup.MapGet("/roles", IResult () => Results.Ok(new[] { "admin" }));

        adminGroup.MapPost("/users/{userId}/roles/{role}", async Task<IResult> (string userId, string role, IUserStore users, ITokenStore tokenStore) =>
        {
            if (!users.AddRole(userId, role)) return Results.NotFound();
            await RevokeSessionsForUserAsync(userId, users, tokenStore);
            return Results.NoContent();
        });

        adminGroup.MapDelete("/users/{userId}/roles/{role}", async Task<IResult> (string userId, string role, IUserStore users, ITokenStore tokenStore) =>
        {
            if (!users.RemoveRole(userId, role)) return Results.NotFound();
            await RevokeSessionsForUserAsync(userId, users, tokenStore);
            return Results.NoContent();
        });

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
    }

    private static async Task<OAuthTokenResponse?> IssueTokenAsync(string username, string password, IUserStore users, IPasswordHasher<User> hasher, ITokenStore tokenStore)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;

        var user = users.FindByUsername(username.Trim());
        if (user is null || !user.Enabled) return null;

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed) return null;

        var accessToken = Guid.NewGuid().ToString("N");
        var refreshToken = Guid.NewGuid().ToString("N");
        var accessLifetime = TimeSpan.FromHours(1);
        var refreshLifetime = TimeSpan.FromDays(7);
        var roles = user.Roles.ToArray();

        await tokenStore.StoreAccessTokenAsync(accessToken, new TokenInfo
        {
            Username = user.Username,
            Roles = roles,
            RefreshTokenHash = TokenHashing.Compute(refreshToken),
            Expiry = DateTime.UtcNow.Add(accessLifetime)
        }, accessLifetime);
        await tokenStore.StoreRefreshSessionAsync(
            refreshToken,
            new RefreshSession(user.Id, user.Username, roles, DateTime.UtcNow.Add(refreshLifetime)),
            refreshLifetime);

        return new OAuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "bearer",
            ExpiresIn = (int)accessLifetime.TotalSeconds,
            Username = user.Username,
            Roles = roles,
            ExpiresAt = DateTimeOffset.UtcNow.Add(accessLifetime).ToString("o")
        };
    }

    private static async Task RevokeSessionsForUserAsync(string userId, IUserStore users, ITokenStore tokenStore)
    {
        var user = users.FindById(userId);
        if (user is not null)
            await tokenStore.RevokeAllSessionsForUserAsync(user.Id, user.Username);
    }
}

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
