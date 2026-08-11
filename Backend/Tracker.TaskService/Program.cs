using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tracker.TaskService.Endpoints;
using Tracker.TaskService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var authority = jwtSettings["Authority"]
            ?? throw new InvalidOperationException("JwtSettings:Authority is not configured.");
        var audience = jwtSettings["Audience"] ?? "account";

        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = jwtSettings.GetValue<bool>("RequireHttpsMetadata", true);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal;
                if (principal is null)
                {
                    return Task.CompletedTask;
                }

                var identity = principal.Identity as ClaimsIdentity;
                if (identity is null)
                {
                    return Task.CompletedTask;
                }

                var realmAccessValue = principal.FindFirst("realm_access")?.Value;
                if (string.IsNullOrWhiteSpace(realmAccessValue))
                {
                    return Task.CompletedTask;
                }

                using var realmAccess = JsonDocument.Parse(realmAccessValue);
                if (!realmAccess.RootElement.TryGetProperty("roles", out var rolesElement) || rolesElement.ValueKind != JsonValueKind.Array)
                {
                    return Task.CompletedTask;
                }

                foreach (var role in rolesElement.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (!string.IsNullOrWhiteSpace(roleName) && !identity.HasClaim(ClaimTypes.Role, roleName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = (builder.Configuration["AllowedFrontendOrigins"]
                        ?? "http://localhost:4200,http://localhost:4300")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();

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