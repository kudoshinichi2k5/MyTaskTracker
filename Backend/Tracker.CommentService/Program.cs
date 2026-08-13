using Tracker.CommentService.Auth;
using Tracker.CommentService.Models;
using Tracker.CommentService.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Auth: opaque bearer token validated against Tracker.AuthService /verify.
// No JWT is issued or parsed anywhere in this service.
builder.Services.AddHttpClient("AuthServiceVerify");

builder.Services
    .AddAuthentication(OpaqueTokenAuthOptions.SchemeName)
    .AddScheme<OpaqueTokenAuthOptions, OpaqueTokenAuthenticationHandler>(
        OpaqueTokenAuthOptions.SchemeName,
        options =>
        {
            options.AuthServiceBaseUrl = builder.Configuration["AuthService:BaseUrl"]
                ?? "http://localhost:5001";
            options.VerifyPath = builder.Configuration["AuthService:VerifyPath"] ?? "/verify";
        });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<CommentStore>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true)
    .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Tracker.CommentService" }))
   .AllowAnonymous();

// Comments scoped to a task
app.MapGet("/api/v1/tasks/{taskId:guid}/comments", (Guid taskId, CommentStore store) =>
{
    var result = store.GetForTask(taskId).Select(CommentResponse.FromEntity);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapPost("/api/v1/tasks/{taskId:guid}/comments", (
    Guid taskId, CreateCommentRequest request, HttpContext ctx, CommentStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Body))
    {
        return Results.BadRequest(new { error = "Body is required." });
    }

    var userId = ctx.User.GetUserId();
    var created = store.Add(taskId, userId, request.Body.Trim());
    return Results.Created(
        $"/api/v1/tasks/{taskId}/comments/{created.Id}",
        CommentResponse.FromEntity(created));
}).RequireAuthorization();

// Direct comment operations (edit/delete by the comment's own id)
app.MapPut("/api/v1/comments/{commentId:guid}", (
    Guid commentId, UpdateCommentRequest request, HttpContext ctx, CommentStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Body))
    {
        return Results.BadRequest(new { error = "Body is required." });
    }

    var updated = store.Update(commentId, ctx.User.GetUserId(), request.Body.Trim());
    return updated is null ? Results.NotFound() : Results.Ok(CommentResponse.FromEntity(updated));
}).RequireAuthorization();

app.MapDelete("/api/v1/comments/{commentId:guid}", (Guid commentId, HttpContext ctx, CommentStore store) =>
{
    var deleted = store.Delete(commentId, ctx.User.GetUserId());
    return deleted ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

app.Run();

static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this System.Security.Claims.ClaimsPrincipal user) =>
        user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing a user id claim.");
}
