using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tracker.TaskService.Endpoints;
using Tracker.TaskService.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình JWT Bearer Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["JwtSettings:Authority"];
        options.Audience = builder.Configuration["JwtSettings:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("JwtSettings:RequireHttpsMetadata");

        // Keycloak puts realm roles under a custom "realm_access": { "roles": [...] }
        // claim, not the standard ClaimTypes.Role that ASP.NET Core's
        // [Authorize(Roles=...)] / RequireRole() policies expect. Unpack it once
        // here so both the "AdminOnly" policy below and any future role checks
        // work without every endpoint re-parsing the token by hand.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var realmAccessJson = context.Principal?.FindFirst("realm_access")?.Value;
                if (string.IsNullOrEmpty(realmAccessJson) || context.Principal?.Identity is not ClaimsIdentity identity)
                {
                    return Task.CompletedTask;
                }

                using var doc = JsonDocument.Parse(realmAccessJson);
                if (doc.RootElement.TryGetProperty("roles", out var roles))
                {
                    foreach (var role in roles.EnumerateArray())
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                    }
                }

                return Task.CompletedTask;
            }
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

app.Run();