using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tracker.TaskService.Endpoints;
using Tracker.TaskService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
            // Khớp claim role cho [Authorize(Roles = "...")]
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Only Keycloak users holding the "admin" realm role (assigned to
    // directors/managers) can call /api/v1/tasks/admin/*. Regular employees
    // using the Customer App never have this role, so those endpoints 403
    // for them even if they guess the URL - enforced server-side, not just
    // hidden in the Admin Portal's UI.
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Two separate Angular apps (Customer App + Admin Portal) call this
        // API now, so more than one origin needs to be allowed per
        // environment. Comma-separated so a single appsettings key still
        // covers it: "http://localhost:4200,http://localhost:4300".
        var origins = (builder.Configuration["AllowedFrontendOrigins"]
                        ?? "http://localhost:4200,http://localhost:4300")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// In-memory task store, scoped per user.
builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();

// Powers the Admin Portal's Users/Roles screens - see Services/
// KeycloakAdminClient.cs for why this can't just be called from the
// Angular app directly.
builder.Services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>();

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

app.MapTaskEndpoints();
app.MapAdminUserEndpoints();

app.Run();