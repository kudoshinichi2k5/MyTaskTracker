using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tracker.NotificationService.Endpoints;
using Tracker.NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

var authServiceBaseUrl = builder.Configuration["AuthService:BaseUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient("AuthService", client => client.BaseAddress = new Uri(authServiceBaseUrl));

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, OwinStyleAuthHandler>("Bearer", null);
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = (builder.Configuration["AllowedFrontendOrigins"]
                        ?? "http://localhost:4200,http://localhost:4300")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
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

public sealed class OwinStyleAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly HttpClient _httpClient;

    public OwinStyleAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, IHttpClientFactory httpClientFactory) : base(options, logger, encoder) =>
        _httpClient = httpClientFactory.CreateClient("AuthService");

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.Fail("Bearer token is empty.");

        try
        {
            using var response = await _httpClient.GetAsync($"/verify?token={Uri.EscapeDataString(token)}", Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
                return AuthenticateResult.Fail("Auth service token verification failed.");

            var result = await response.Content.ReadFromJsonAsync<IntrospectionResponse>(Context.RequestAborted);
            if (result is null || !result.Active || string.IsNullOrWhiteSpace(result.Username))
                return AuthenticateResult.Fail("Token is invalid or expired.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, result.Username),
                new(ClaimTypes.NameIdentifier, result.Username)
            };
            claims.AddRange(result.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
        }
        catch (HttpRequestException exception)
        {
            Logger.LogWarning(exception, "Auth service is unavailable while validating a bearer token.");
            return AuthenticateResult.Fail("Auth service is unavailable.");
        }
    }
}

public sealed class IntrospectionResponse
{
    public bool Active { get; set; }
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
