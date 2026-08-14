using Tracker.ProjectService.Auth;
using Tracker.ProjectService.Endpoints;
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
            options.AuthServiceBaseUrl =
                builder.Configuration["AuthService:BaseUrl"]
                ?? "http://localhost:5001";

            options.VerifyPath =
                builder.Configuration["AuthService:VerifyPath"]
                ?? "/verify";
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

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "healthy",
        service = "Tracker.ProjectService"
    }))
    .AllowAnonymous();

app.MapProjectEndpoints();

app.Run();