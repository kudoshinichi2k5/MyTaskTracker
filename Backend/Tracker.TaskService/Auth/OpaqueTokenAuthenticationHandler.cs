using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Tracker.TaskService.Auth;

public sealed class OpaqueTokenAuthenticationHandler
    : AuthenticationHandler<OpaqueTokenAuthOptions>
{
    private readonly HttpClient _httpClient;

    public OpaqueTokenAuthenticationHandler(
        IOptionsMonitor<OpaqueTokenAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory)
        : base(options, logger, encoder)
    {
        _httpClient = httpClientFactory.CreateClient("AuthServiceVerify");
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();

        if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Bearer token is empty.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                Options.VerifyPath);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(
                request,
                Context.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                return AuthenticateResult.Fail(
                    "Auth service token verification failed.");
            }

            var result =
                await response.Content.ReadFromJsonAsync<IntrospectionResponse>(
                    Context.RequestAborted);

            if (result is null ||
                !result.Active ||
                string.IsNullOrWhiteSpace(result.Username))
            {
                return AuthenticateResult.Fail(
                    "Token is invalid or expired.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, result.Username),
                new(ClaimTypes.NameIdentifier, result.Username)
            };

            claims.AddRange(
                result.Roles.Select(
                    role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(
                claims,
                Scheme.Name,
                ClaimTypes.Name,
                ClaimTypes.Role);

            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(
                principal,
                Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (HttpRequestException exception)
        {
            Logger.LogWarning(
                exception,
                "Auth service is unavailable while validating a bearer token.");

            return AuthenticateResult.Fail(
                "Auth service is unavailable.");
        }
    }
}

public sealed class IntrospectionResponse
{
    public bool Active { get; set; }

    public string Username { get; set; } = string.Empty;

    public string[] Roles { get; set; } = Array.Empty<string>();
}