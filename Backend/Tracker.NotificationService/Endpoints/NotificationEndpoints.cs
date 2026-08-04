using System.Security.Claims;
using Tracker.NotificationService.Services;

namespace Tracker.NotificationService.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").RequireAuthorization();

        group.MapGet("/", (ClaimsPrincipal user, INotificationStore store) =>
            Results.Ok(store.GetAll(user.GetUserId())));

        // Lets the frontend show an unread badge without downloading the full list.
        group.MapGet("/unread-count", (ClaimsPrincipal user, INotificationStore store) =>
            Results.Ok(new { count = store.GetUnreadCount(user.GetUserId()) }));

        group.MapPost("/{id:int}/read", (int id, ClaimsPrincipal user, INotificationStore store) =>
        {
            var updated = store.MarkAsRead(user.GetUserId(), id);
            return updated ? Results.NoContent() : Results.NotFound();
        });
    }

    private static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated request is missing a subject claim.");
}