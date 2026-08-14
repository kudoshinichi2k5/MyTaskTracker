using System.Security.Claims;
using Tracker.CommentService.Models;
using Tracker.CommentService.Services;

namespace Tracker.CommentService.Endpoints;

public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        // Comments scoped to a task
        var taskComments = app.MapGroup("/api/v1/tasks/{taskId:guid}/comments")
            .RequireAuthorization();

        taskComments.MapGet("/", (Guid taskId, CommentStore store) =>
        {
            var result = store
                .GetForTask(taskId)
                .Select(CommentResponse.FromEntity);

            return Results.Ok(result);
        });

        taskComments.MapPost("/", (
            Guid taskId,
            CreateCommentRequest request,
            HttpContext ctx,
            CommentStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return Results.BadRequest(new { error = "Body is required." });
            }

            var userId = ctx.User.GetUserId();

            var created = store.Add(
                taskId,
                userId,
                request.Body.Trim());

            return Results.Created(
                $"/api/v1/tasks/{taskId}/comments/{created.Id}",
                CommentResponse.FromEntity(created));
        });

        // Direct comment operations
        // Edit/delete by the comment's own id.
        var comments = app.MapGroup("/api/v1/comments")
            .RequireAuthorization();

        comments.MapPut("/{commentId:guid}", (
            Guid commentId,
            UpdateCommentRequest request,
            HttpContext ctx,
            CommentStore store) =>
        {
            if (string.IsNullOrWhiteSpace(request.Body))
            {
                return Results.BadRequest(new { error = "Body is required." });
            }

            var updated = store.Update(
                commentId,
                ctx.User.GetUserId(),
                request.Body.Trim());

            return updated is null
                ? Results.NotFound()
                : Results.Ok(CommentResponse.FromEntity(updated));
        });

        comments.MapDelete("/{commentId:guid}", (
            Guid commentId,
            HttpContext ctx,
            CommentStore store) =>
        {
            var deleted = store.Delete(
                commentId,
                ctx.User.GetUserId());

            return deleted
                ? Results.NoContent()
                : Results.NotFound();
        });
    }

    private static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException(
            "Authenticated request is missing a subject claim.");
}