using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tracker.NotificationService.Endpoints;
using Tracker.NotificationService.Services;

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

// In-memory notification store, scoped per user (see InMemoryNotificationStore
// for the persistence caveat).
builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();

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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", environment = app.Environment.EnvironmentName }));

app.MapNotificationEndpoints();

app.Run();