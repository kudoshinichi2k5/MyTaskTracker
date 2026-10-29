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
        // Account creation is not an OAuth grant. The SPA starts the
        // authorization-code flow after this succeeds, so this route never
        // returns bearer tokens or handles a password grant.
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

            var user = new User { Username = username, Email = email, PasswordHash = string.Empty };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            users.Create(user);

            return Results.Created($"/api/v1/auth/users/{user.Id}", new { username = user.Username });
        });

        // Resource services continue to validate opaque bearer tokens here.
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

    private static async Task RevokeSessionsForUserAsync(string userId, IUserStore users, ITokenStore tokenStore)
    {
        var user = users.FindById(userId);
        if (user is not null)
            await tokenStore.RevokeAllSessionsForUserAsync(user.Id, user.Username);
    }
}

public record RegisterRequest(string Username, string Email, string Password);
