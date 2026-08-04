using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Same JWT Bearer setup as Tracker.TaskService: both services validate
// tokens issued by the same Keycloak realm, so a user only signs in once
// (via the frontend's SSO flow) and the resulting token is accepted by
// every backend microservice.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["JwtSettings:Authority"];
        options.Audience = builder.Configuration["JwtSettings:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("JwtSettings:RequireHttpsMetadata");
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";

        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// In-memory mock data, same pattern as Tracker.TaskService until a real
// data store (and a real event source - e.g. task-due-soon, task-assigned)
// is wired in.
var notifications = new List<NotificationItem>
{
    new NotificationItem { Id = 1, Message = "Welcome to TaskTracker! Your workspace is ready.", IsRead = true },
    new NotificationItem { Id = 2, Message = "\"Set up local IIS\" is still open.", IsRead = false },
    new NotificationItem { Id = 3, Message = "\"Learn microservices\" was marked complete.", IsRead = false }
};

app.MapGet("/health", () => Results.Ok(new { status = "healthy", environment = app.Environment.EnvironmentName }));

// List notifications for the signed-in user (BẮT BUỘC CÓ TOKEN).
app.MapGet("/api/v1/notifications", () =>
{
    return Results.Ok(notifications);
}).RequireAuthorization();

// Mark a single notification as read.
app.MapPost("/api/v1/notifications/{id:int}/read", (int id) =>
{
    var notification = notifications.FirstOrDefault(n => n.Id == id);
    if (notification is null)
    {
        return Results.NotFound();
    }

    notification.IsRead = true;
    return Results.NoContent();
}).RequireAuthorization();

app.Run();

public class NotificationItem
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}