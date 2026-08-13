using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tracker.TaskService.Endpoints;
using Tracker.TaskService.Services;

var builder = WebApplication.CreateBuilder(args);

var authServiceBaseUrl = builder.Configuration["AuthService:BaseUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri(authServiceBaseUrl);
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
        var configuredOrigins = (builder.Configuration["AllowedFrontendOrigins"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(origin => origin.TrimEnd('/'));

        var localOrigins = new[]
        {
            "http://localhost:4200",
            "http://localhost:4300",
            "http://app.testing.local"
        };

        policy.WithOrigins(configuredOrigins.Concat(localOrigins).Distinct().ToArray())
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

// Respects the "Urls" key in appsettings.{Environment}.json / ASPNETCORE_URLS
// instead of forcing every environment (and the IIS in-process host) onto
// localhost:5002.
app.Run();

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
            return AuthenticateResult.NoResult();
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Invalid Authorization Scheme");
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("Bearer token is empty.");
        }

        HttpResponseMessage response;
        try
        {
            // Token travels in the Authorization header, not a query string -
            // IIS logs full request URLs (including query strings) by
            // default, which would have written every access token used
            // anywhere in the system into Tracker.AuthService's own access
            // logs.
            using var request = new HttpRequestMessage(HttpMethod.Get, "verify");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            response = await _httpClient.SendAsync(request, Context.RequestAborted);
        }
        catch (HttpRequestException exception)
        {
            Logger.LogWarning(exception, "Auth service is unavailable while validating a bearer token.");
            return AuthenticateResult.Fail("Auth service is unavailable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return AuthenticateResult.Fail("Auth Service returned an error.");
        }

        var result = await response.Content.ReadFromJsonAsync<IntrospectionResponse>();
        if (result is null || !result.Active)
        {
            return AuthenticateResult.Fail("Token invalid or expired");
        }

        // AuthService only knows a username, not a separate opaque user id, so
        // the username doubles as the subject/"sub" claim. ITaskStore keys its
        // per-user buckets off this claim, and RequireAuthorization("AdminOnly")
        // depends on the role claims below.
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, result.Username),
            new(ClaimTypes.NameIdentifier, result.Username)
        };
        claims.AddRange(result.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}

public class IntrospectionResponse
{
    public bool Active { get; set; }
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
