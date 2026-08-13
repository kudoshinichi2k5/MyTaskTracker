using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Tracker.CommentService.Auth;

/// <summary>
/// Custom ASP.NET Core authentication handler that validates the opaque
/// bearer token by calling the Auth Service's GET /verify?token=... endpoint.
/// This intentionally replaces JWT validation: the token carries no claims
/// itself, it is only a lookup key the Auth Service resolves server-side.
/// </summary>
public sealed class OpaqueTokenAuthenticationHandler : AuthenticationHandler<OpaqueTokenAuthOptions>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OpaqueTokenAuthenticationHandler(
        IOptionsMonitor<OpaqueTokenAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory)
        : base(options, logger, encoder)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var rawHeader = authHeader.ToString();
        if (string.IsNullOrWhiteSpace(rawHeader) ||
            !rawHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = rawHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("Empty bearer token.");
        }

        var client = _httpClientFactory.CreateClient("AuthServiceVerify");
        client.BaseAddress = new Uri(Options.AuthServiceBaseUrl);

        HttpResponseMessage response;
        try
        {
            var verifyUrl = $"{Options.VerifyPath}?token={Uri.EscapeDataString(token)}";
            response = await client.GetAsync(verifyUrl);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Unable to reach Auth Service for token verification.");
            return AuthenticateResult.Fail("Auth Service is unreachable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return AuthenticateResult.Fail("Token rejected by Auth Service.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        VerifyResponse? verified;
        try
        {
            verified = await JsonSerializer.DeserializeAsync<VerifyResponse>(
                stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Malformed response from Auth Service /verify.");
            return AuthenticateResult.Fail("Malformed verification response.");
        }

        if (verified is null || string.IsNullOrEmpty(verified.UserId))
        {
            return AuthenticateResult.Fail("Token did not resolve to a user.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, verified.UserId),
            new(ClaimTypes.Name, verified.Username ?? verified.UserId)
        };

        if (!string.IsNullOrEmpty(verified.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, verified.Role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private sealed class VerifyResponse
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
    }
}
