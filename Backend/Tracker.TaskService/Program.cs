using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tracker.TaskService.Endpoints;
using Tracker.TaskService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
});

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, OwinStyleAuthHandler>("Bearer", null);

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

app.Run("http://localhost:5002");

public class OwinStyleAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly HttpClient _httpClient;

    public OwinStyleAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory)
        : base(options, logger, encoder)
    {
        _httpClient = httpClientFactory.CreateClient("AuthService");
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.Fail("Missing Authorization Header");
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Invalid Authorization Scheme");
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var response = await _httpClient.GetAsync($"/verify?token={Uri.EscapeDataString(token)}");

        if (!response.IsSuccessStatusCode)
        {
            return AuthenticateResult.Fail("Auth Service is unreachable");
        }

        var result = await response.Content.ReadFromJsonAsync<IntrospectionResponse>();
        if (result is null || !result.Active)
        {
            return AuthenticateResult.Fail("Token invalid or expired");
        }

        var claims = new[] { new Claim(ClaimTypes.Name, result.Username) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}

public class IntrospectionResponse
{
    public bool Active { get; set; }
    public string Username { get; set; } = string.Empty;
}