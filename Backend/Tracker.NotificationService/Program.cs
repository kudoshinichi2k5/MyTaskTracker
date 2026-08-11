using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tracker.NotificationService.Endpoints;
using Tracker.NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"] ?? "http://localhost:5001";
var audience = jwtSection["Audience"] ?? "task-tracker-clients";
var secretKey = jwtSection["SecretKey"] ?? "SuperSecretKeyForTaskTrackerSystemDoNotShare2026!";
var signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = signingKey,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

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