using Microsoft.AspNetCore.Authentication;
using Tracker.ProjectService.Auth;
using Tracker.ProjectService.Models;
using Tracker.ProjectService.Services;

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

builder.Services.AddSingleton<ProjectStore>();
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Tracker.ProjectService" }))
   .AllowAnonymous();

var projects = app.MapGroup("/api/v1/projects").RequireAuthorization();

projects.MapGet("/", (HttpContext ctx, ProjectStore store) =>
{
    var userId = ctx.User.GetUserId();
    var result = store.GetAllForUser(userId).Select(ProjectResponse.FromEntity);
    return Results.Ok(result);
});

projects.MapGet("/{id:guid}", (Guid id, HttpContext ctx, ProjectStore store) =>
{
    var project = store.GetById(id);
    if (project is null || project.OwnerUserId != ctx.User.GetUserId())
    {
        return Results.NotFound();
    }

    return Results.Ok(ProjectResponse.FromEntity(project));
});

projects.MapPost("/", (CreateProjectRequest request, HttpContext ctx, ProjectStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required." });
    }

    var userId = ctx.User.GetUserId();
    var created = store.Create(userId, request.Name.Trim(), request.Description);
    return Results.Created($"/api/v1/projects/{created.Id}", ProjectResponse.FromEntity(created));
});

projects.MapPut("/{id:guid}", (Guid id, UpdateProjectRequest request, HttpContext ctx, ProjectStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required." });
    }

    var updated = store.Update(id, ctx.User.GetUserId(), request.Name.Trim(), request.Description);
    return updated is null ? Results.NotFound() : Results.Ok(ProjectResponse.FromEntity(updated));
});

projects.MapDelete("/{id:guid}", (Guid id, HttpContext ctx, ProjectStore store) =>
{
    var deleted = store.Delete(id, ctx.User.GetUserId());
    return deleted ? Results.NoContent() : Results.NotFound();
});

projects.MapPost("/{id:guid}/tasks/{taskId:guid}", (Guid id, Guid taskId, HttpContext ctx, ProjectStore store) =>
{
    var updated = store.AttachTask(id, ctx.User.GetUserId(), taskId);
    return updated is null ? Results.NotFound() : Results.Ok(ProjectResponse.FromEntity(updated));
});

projects.MapDelete("/{id:guid}/tasks/{taskId:guid}", (Guid id, Guid taskId, HttpContext ctx, ProjectStore store) =>
{
    var updated = store.DetachTask(id, ctx.User.GetUserId(), taskId);
    return updated is null ? Results.NotFound() : Results.Ok(ProjectResponse.FromEntity(updated));
});

app.Run();

static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this System.Security.Claims.ClaimsPrincipal user) =>
        user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing a user id claim.");
}
