using System.Security.Claims;
using Tracker.ProjectService.Models;
using Tracker.ProjectService.Services;

namespace Tracker.ProjectService.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var projects = app
            .MapGroup("/api/v1/projects")
            .RequireAuthorization();

        projects.MapGet("/", (
            HttpContext ctx,
            ProjectStore store) =>
        {
            var userId = ctx.User.GetUserId();

            var result = store
                .GetAllForUser(userId)
                .Select(ProjectResponse.FromEntity);

            return Results.Ok(result);
        });

        projects.MapGet("/{id:guid}", (
            Guid id,
            HttpContext ctx,
            ProjectStore store) =>
        {
            var project = store.GetById(id);

            if (project is null ||
                project.OwnerUserId != ctx.User.GetUserId())
            {
                return Results.NotFound();
            }

            return Results.Ok(
                ProjectResponse.FromEntity(project));
        });

        projects.MapPost("/", (
            CreateProjectRequest request,
            HttpContext ctx,
            ProjectStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(
                    new { error = "Name is required." });
            }

            var userId = ctx.User.GetUserId();

            var created = store.Create(
                userId,
                request.Name.Trim(),
                request.Description);

            return Results.Created(
                $"/api/v1/projects/{created.Id}",
                ProjectResponse.FromEntity(created));
        });

        projects.MapPut("/{id:guid}", (
            Guid id,
            UpdateProjectRequest request,
            HttpContext ctx,
            ProjectStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(
                    new { error = "Name is required." });
            }

            var updated = store.Update(
                id,
                ctx.User.GetUserId(),
                request.Name.Trim(),
                request.Description);

            return updated is null
                ? Results.NotFound()
                : Results.Ok(
                    ProjectResponse.FromEntity(updated));
        });

        projects.MapDelete("/{id:guid}", (
            Guid id,
            HttpContext ctx,
            ProjectStore store) =>
        {
            var deleted = store.Delete(
                id,
                ctx.User.GetUserId());

            return deleted
                ? Results.NoContent()
                : Results.NotFound();
        });

        projects.MapPost(
            "/{id:guid}/tasks/{taskId:int}",
            (
                Guid id,
                int taskId,
                HttpContext context,
                ProjectStore store) =>
            {
                var userId =
                    context.User.GetUserId();

                var project =
                    store.AttachTask(
                        id,
                        userId,
                        taskId);

                return project is null
                    ? Results.NotFound()
                    : Results.Ok(
                        ProjectResponse.FromEntity(
                            project));
            })
            .RequireAuthorization();

        projects.MapDelete(
            "/{id:guid}/tasks/{taskId:int}",
            (
                Guid id,
                int taskId,
                HttpContext context,
                ProjectStore store) =>
            {
                var userId =
                    context.User.GetUserId();

                var project =
                    store.DetachTask(
                        id,
                        userId,
                        taskId);

                return project is null
                    ? Results.NotFound()
                    : Results.Ok(
                        ProjectResponse.FromEntity(
                            project));
            })
            .RequireAuthorization();
    }

    private static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException(
            "Authenticated request is missing a subject claim.");
}