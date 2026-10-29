namespace Tracker.AuthService.Services;

/// <summary>
/// Public SPA clients are identified by exact, pre-registered redirect URIs.
/// A public client has no client secret; PKCE is its proof at token exchange.
/// </summary>
public sealed class OAuthClientRegistry
{
    private readonly IConfiguration _configuration;

    public OAuthClientRegistry(IConfiguration configuration) => _configuration = configuration;

    public bool IsValidRedirectUri(string clientId, string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !IsLocalHttp(uri)))
        {
            return false;
        }

        var allowedUris = _configuration
            .GetSection($"OAuth:Clients:{clientId}:RedirectUris")
            .Get<string[]>() ?? Array.Empty<string>();

        return allowedUris.Contains(redirectUri, StringComparer.Ordinal);
    }

    private static bool IsLocalHttp(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttp &&
        (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.IsLoopback);
}
