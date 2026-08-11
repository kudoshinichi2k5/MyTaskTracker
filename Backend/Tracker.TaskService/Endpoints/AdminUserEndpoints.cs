using Tracker.TaskService.Models;

namespace Tracker.TaskService.Endpoints;

public static class AdminUserEndpoints
{
    private static readonly List<AdminUserSummary> SeedUsers =
    [
        new("admin-user-id", "admin", "admin@tasktracker.local", true, ["admin", "user"]),
        new("user-id", "user", "user@tasktracker.local", true, ["user"])
    ];

    private static readonly List<string> SeedRoles = ["admin", "user"];

    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin").RequireAuthorization("AdminOnly");

        group.MapGet("/users", () => Results.Ok(SeedUsers));

        group.MapGet("/roles", () => Results.Ok(SeedRoles));

        group.MapPost("/users/{userId}/roles/{roleName}", (string userId, string roleName) =>
        {
            var user = SeedUsers.FirstOrDefault(u => u.Id == userId);
            if (user is null) return Results.NotFound();
            if (!SeedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase)) return Results.BadRequest();
            if (!user.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            {
                var updatedRoles = new List<string>(user.Roles) { roleName };
                var index = SeedUsers.IndexOf(user);
                SeedUsers[index] = user with { Roles = updatedRoles };
            }

            return Results.NoContent();
        });

        group.MapDelete("/users/{userId}/roles/{roleName}", (string userId, string roleName) =>
        {
            var user = SeedUsers.FirstOrDefault(u => u.Id == userId);
            if (user is null) return Results.NotFound();
            var updatedRoles = user.Roles.Where(r => !string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase)).ToList();
            var index = SeedUsers.IndexOf(user);
            SeedUsers[index] = user with { Roles = updatedRoles };
            return Results.NoContent();
        });
    }
}
