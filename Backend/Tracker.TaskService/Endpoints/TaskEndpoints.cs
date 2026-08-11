using System.Security.Claims;
using Tracker.TaskService.Services;

namespace Tracker.TaskService.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tasks").RequireAuthorization();

        group.MapGet("/", (ClaimsPrincipal user, ITaskStore store) =>
            Results.Ok(store.GetAll(user.GetUserId())));

        group.MapPost("/", (ClaimsPrincipal user, ITaskStore store, CreateTaskRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = ["Title is required."]
                });
            }

            var task = store.Create(user.GetUserId(), request.Title.Trim());
            return Results.Created($"/api/v1/tasks/{task.Id}", task);
        });

        group.MapPut("/{id:int}", (int id, ClaimsPrincipal user, ITaskStore store, UpdateTaskRequest request) =>
        {
            if (request.Title is not null && string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = ["Title cannot be empty."]
                });
            }

            var updated = store.Update(user.GetUserId(), id, request.Title?.Trim(), request.IsCompleted);
            return updated ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:int}", (int id, ClaimsPrincipal user, ITaskStore store) =>
        {
            var deleted = store.Delete(user.GetUserId(), id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        // Admin Portal only: aggregate view across every user's tasks.
        // "AdminOnly" is enforced server-side via the "admin" role in the JWT
        // issued by the AuthService; an ordinary user cannot reach this route.
        var adminGroup = app.MapGroup("/api/v1/tasks/admin").RequireAuthorization("AdminOnly");

        adminGroup.MapGet("/summary", (ITaskStore store) =>
            Results.Ok(store.GetSummaryForAllUsers()));
    }

    // The JWT "sub" claim is the stable user identifier; it is issued by the
    // AuthService and used consistently by the resource services.
    private static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated request is missing a subject claim.");
}

public record CreateTaskRequest(string Title);
public record UpdateTaskRequest(string? Title, bool? IsCompleted);