using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tracker.AuthService.Models;
using Tracker.AuthService.Services;

namespace Tracker.AuthService.Endpoints;

/// <summary>
/// OAuth 2.0 authorization-code endpoints for public SPAs. Access and refresh
/// tokens remain opaque and are persisted only in the AuthService token store.
/// </summary>
public static class OAuthEndpoints
{
    private const string ApiScope = "api";
    private const string AuthorizationTransactionCookie = "oauth_authorization_transaction";
    private static readonly TimeSpan AuthorizationRequestLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AuthorizationCodeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan FailedLoginLifetime = TimeSpan.FromMinutes(5);
    private const int MaxFailedLoginAttempts = 10;

    public static void MapOAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/connect/authorize", async Task<IResult> (
            HttpContext context,
            ITokenStore tokenStore,
            OAuthClientRegistry clients) =>
        {
            var request = context.Request.Query;
            var clientId = request["client_id"].ToString();
            var redirectUri = request["redirect_uri"].ToString();
            var responseType = request["response_type"].ToString();
            var scope = request["scope"].ToString();
            var state = request["state"].ToString();
            var codeChallenge = request["code_challenge"].ToString();
            var codeChallengeMethod = request["code_challenge_method"].ToString();

            if (!string.Equals(responseType, "code", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(clientId) ||
                !clients.IsValidRedirectUri(clientId, redirectUri) ||
                !HasOnlyApiScope(scope) ||
                string.IsNullOrWhiteSpace(state) ||
                !IsValidCodeChallenge(codeChallenge) ||
                !string.Equals(codeChallengeMethod, "S256", StringComparison.Ordinal))
            {
                return Results.BadRequest(new { error = "invalid_request" });
            }

            var transactionId = OAuthSecretGenerator.Create();
            await tokenStore.StoreAuthorizationRequestAsync(
                transactionId,
                new AuthorizationRequest(
                    clientId, redirectUri, ApiScope, state, codeChallenge,
                    DateTime.UtcNow.Add(AuthorizationRequestLifetime)),
                AuthorizationRequestLifetime);

            AppendAuthorizationTransactionCookie(context, transactionId);
            return Results.Content(
                RenderLoginPage(transactionId, registrationUri: RegistrationUri(redirectUri)),
                "text/html; charset=utf-8");
        }).RequireRateLimiting("oauth");

        app.MapPost("/connect/authorize", async Task<IResult> (
            HttpContext context,
            IUserStore users,
            IPasswordHasher<User> hasher,
            ITokenStore tokenStore) =>
        {
            if (!context.Request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "invalid_request" });
            }

            var form = await context.Request.ReadFormAsync();
            var transactionId = form["transaction"].ToString();
            var transactionCookie = context.Request.Cookies[AuthorizationTransactionCookie];
            var authorizationRequest = await tokenStore.GetAuthorizationRequestAsync(transactionId);

            if (authorizationRequest is null || authorizationRequest.ExpiresAt <= DateTime.UtcNow ||
                string.IsNullOrWhiteSpace(transactionCookie) ||
                !FixedTimeEquals(transactionId, transactionCookie))
            {
                return Results.BadRequest(new { error = "invalid_request" });
            }

            var username = form["username"].ToString().Trim();
            var password = form["password"].ToString();
            if (await IsLoginLockedAsync(context, username))
            {
                return Results.Content(
                    RenderLoginPage(
                        transactionId,
                        "Too many failed attempts. Please try again later.",
                        RegistrationUri(authorizationRequest.RedirectUri)),
                    "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status429TooManyRequests);
            }

            var user = users.FindByUsername(username);
            if (user is null || !user.Enabled ||
                hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            {
                await RecordFailedLoginAsync(context, username);
                return Results.Content(
                    RenderLoginPage(
                        transactionId,
                        "Invalid username or password.",
                        RegistrationUri(authorizationRequest.RedirectUri)),
                    "text/html; charset=utf-8",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            await ClearFailedLoginsAsync(context, username);

            // The browser transaction is single-use. This also prevents an
            // authorization request from producing more than one code.
            var consumedRequest = await tokenStore.ConsumeAuthorizationRequestAsync(transactionId);
            if (consumedRequest is null || consumedRequest.ExpiresAt <= DateTime.UtcNow)
            {
                return Results.BadRequest(new { error = "invalid_request" });
            }

            context.Response.Cookies.Delete(AuthorizationTransactionCookie, new CookieOptions { Path = "/" });

            var code = OAuthSecretGenerator.Create();
            await tokenStore.StoreAuthorizationCodeAsync(
                code,
                new AuthorizationCode(
                    consumedRequest.ClientId,
                    consumedRequest.RedirectUri,
                    consumedRequest.CodeChallenge,
                    user.Id,
                    user.Username,
                    user.Roles.ToArray(),
                    DateTime.UtcNow.Add(AuthorizationCodeLifetime)),
                AuthorizationCodeLifetime);

            var callbackUri = QueryHelpers.AddQueryString(
                consumedRequest.RedirectUri,
                new Dictionary<string, string?>
                {
                    ["code"] = code,
                    ["state"] = consumedRequest.State
                });

            return Results.Redirect(callbackUri);
        }).RequireRateLimiting("oauth");

        app.MapPost("/connect/token", async Task<IResult> (
            HttpContext context,
            ITokenStore tokenStore,
            OAuthClientRegistry clients) =>
        {
            if (!context.Request.HasFormContentType)
            {
                return Error("invalid_request", "Form encoded data is required.");
            }

            var form = await context.Request.ReadFormAsync();
            var clientId = form["client_id"].ToString();
            var grantType = form["grant_type"].ToString();

            if (string.Equals(grantType, "authorization_code", StringComparison.Ordinal))
            {
                var code = form["code"].ToString();
                var redirectUri = form["redirect_uri"].ToString();
                var verifier = form["code_verifier"].ToString();
                var authorizationCode = await tokenStore.ConsumeAuthorizationCodeAsync(code);

                if (authorizationCode is null || authorizationCode.ExpiresAt <= DateTime.UtcNow ||
                    !string.Equals(authorizationCode.ClientId, clientId, StringComparison.Ordinal) ||
                    !clients.IsValidRedirectUri(clientId, redirectUri) ||
                    !string.Equals(authorizationCode.RedirectUri, redirectUri, StringComparison.Ordinal) ||
                    !VerifyCodeChallenge(verifier, authorizationCode.CodeChallenge))
                {
                    return Error("invalid_grant", "The authorization code is invalid or expired.");
                }

                return TokenResult(await IssueTokenFromSessionAsync(
                    new RefreshSession(
                        authorizationCode.UserId,
                        authorizationCode.Username,
                        authorizationCode.Roles,
                        DateTime.UtcNow),
                    tokenStore));
            }

            if (string.Equals(grantType, "refresh_token", StringComparison.Ordinal))
            {
                var rawRefreshToken = form["refresh_token"].ToString();
                var session = await tokenStore.ConsumeRefreshSessionAsync(rawRefreshToken);

                if (session is null || session.ExpiresAt <= DateTime.UtcNow)
                {
                    return Error("invalid_grant", "The refresh token is invalid or expired.");
                }

                return TokenResult(await IssueTokenFromSessionAsync(session, tokenStore));
            }

            return Error("unsupported_grant_type");
        }).RequireRateLimiting("oauth");

        app.MapPost("/connect/revocation", async Task<IResult> (
            HttpContext context,
            ITokenStore tokenStore) =>
        {
            if (!context.Request.HasFormContentType)
            {
                return Error("invalid_request", "Form encoded data is required.");
            }

            var rawToken = (await context.Request.ReadFormAsync())["token"].ToString();
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return Results.Ok();
            }

            var accessInfo = await tokenStore.GetAccessTokenAsync(rawToken);
            if (accessInfo is not null)
            {
                await tokenStore.RevokeAccessTokenAsync(rawToken);
                return Results.Ok();
            }

            var refreshSession = await tokenStore.GetRefreshSessionAsync(rawToken);
            if (refreshSession is not null)
            {
                await tokenStore.RevokeRefreshSessionAsync(rawToken);
                await tokenStore.RevokeAccessTokensForRefreshTokenAsync(refreshSession.Username, rawToken);
            }

            return Results.Ok();
        });

        app.MapGet("/.well-known/oauth-authorization-server", () => Results.Ok(new
        {
            issuer = "auth-service",
            authorization_endpoint = "/connect/authorize",
            token_endpoint = "/connect/token",
            revocation_endpoint = "/connect/revocation",
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            code_challenge_methods_supported = new[] { "S256" },
            token_endpoint_auth_methods_supported = new[] { "none" },
            scopes_supported = new[] { ApiScope }
        }));
    }

    private static bool HasOnlyApiScope(string scope) =>
        scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(value => string.Equals(value, ApiScope, StringComparison.Ordinal));

    private static bool IsValidCodeChallenge(string challenge) =>
        challenge.Length is >= 43 and <= 128 && challenge.All(c =>
            char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');

    private static bool VerifyCodeChallenge(string verifier, string expectedChallenge)
    {
        if (verifier.Length is < 43 or > 128 || verifier.Any(c =>
                !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~')))
        {
            return false;
        }

        var actualChallenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return FixedTimeEquals(actualChallenge, expectedChallenge);
    }

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

    private static void AppendAuthorizationTransactionCookie(HttpContext context, string transactionId) =>
        context.Response.Cookies.Append(AuthorizationTransactionCookie, transactionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = RequiresSecureCookie(context),
            SameSite = SameSiteMode.Lax,
            MaxAge = AuthorizationRequestLifetime,
            // The ingress exposes AuthService below /auth, while the service
            // itself receives /connect. Root scope works for both URLs.
            Path = "/"
        });

    private static bool RequiresSecureCookie(HttpContext context)
    {
        var host = context.Request.Host.Host;
        return !host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
            (!IPAddress.TryParse(host, out var address) || !IPAddress.IsLoopback(address));
    }

    private static async Task<bool> IsLoginLockedAsync(HttpContext context, string username)
    {
        var database = context.RequestServices.GetService<IConnectionMultiplexer>()?.GetDatabase();
        if (database is null || string.IsNullOrWhiteSpace(username)) return false;

        var attempts = await database.StringGetAsync(FailedLoginKey(username));
        return int.TryParse(attempts, out var count) && count >= MaxFailedLoginAttempts;
    }

    private static async Task RecordFailedLoginAsync(HttpContext context, string username)
    {
        var database = context.RequestServices.GetService<IConnectionMultiplexer>()?.GetDatabase();
        if (database is null || string.IsNullOrWhiteSpace(username)) return;

        var key = FailedLoginKey(username);
        await database.StringIncrementAsync(key);
        await database.KeyExpireAsync(key, FailedLoginLifetime);
    }

    private static async Task ClearFailedLoginsAsync(HttpContext context, string username)
    {
        var database = context.RequestServices.GetService<IConnectionMultiplexer>()?.GetDatabase();
        if (database is not null && !string.IsNullOrWhiteSpace(username))
        {
            await database.KeyDeleteAsync(FailedLoginKey(username));
        }
    }

    private static string FailedLoginKey(string username) =>
        $"bruteforce:username:{username.Trim().ToLowerInvariant()}";

    private static string RegistrationUri(string redirectUri)
    {
        var callbackUri = new Uri(redirectUri);
        return new UriBuilder(callbackUri)
        {
            Path = "/register",
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri.ToString();
    }

    private static string RenderLoginPage(
        string transactionId,
        string? error = null,
        string? registrationUri = null)
    {
        var encoder = HtmlEncoder.Default;
        var errorMarkup = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"<p class=\"error\" role=\"alert\">{encoder.Encode(error)}</p>";
        var registrationMarkup = string.IsNullOrWhiteSpace(registrationUri)
            ? string.Empty
            : $"<p>New here? <a href=\"{encoder.Encode(registrationUri)}\">Create an account</a></p>";

        var pageStart = """
            <!doctype html>
            <html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Sign in</title><style>body{font-family:system-ui,sans-serif;background:#f5f7fb;margin:0;display:grid;min-height:100vh;place-items:center}main{background:#fff;padding:2rem;border-radius:.75rem;width:min(24rem,calc(100% - 2rem));box-shadow:0 8px 24px #0002}label,input,button{display:block;width:100%;box-sizing:border-box}label{margin-top:1rem}input{padding:.65rem;margin-top:.35rem}button{margin-top:1.5rem;padding:.7rem;background:#155eef;color:#fff;border:0;border-radius:.4rem;font-weight:600}.error{color:#b42318}</style></head>
            <body><main><h1>Sign in</h1>
            """;
        var pageEnd = """
            "><label for="username">Username</label><input id="username" name="username" autocomplete="username" required>
            <label for="password">Password</label><input id="password" name="password" type="password" autocomplete="current-password" required>
            <button type="submit">Sign in</button></form>
            """;

        return pageStart + errorMarkup +
            "<form method=\"post\" action=\"authorize\"><input type=\"hidden\" name=\"transaction\" value=\"" +
            encoder.Encode(transactionId) + pageEnd + registrationMarkup + "</main></body></html>";
    }

    private static IResult Error(string error, string? description = null) =>
        Results.Json(new { error, error_description = description }, statusCode: StatusCodes.Status400BadRequest);

    private static IResult TokenResult(OAuthTokenResponse token) => Results.Ok(new
    {
        access_token = token.AccessToken,
        refresh_token = token.RefreshToken,
        token_type = token.TokenType,
        expires_in = token.ExpiresIn,
        accessToken = token.AccessToken,
        refreshToken = token.RefreshToken,
        tokenType = token.TokenType,
        expiresAt = token.ExpiresAt,
        username = token.Username,
        roles = token.Roles
    });

    private static async Task<OAuthTokenResponse> IssueTokenFromSessionAsync(
        RefreshSession session,
        ITokenStore tokenStore)
    {
        var accessToken = OAuthSecretGenerator.Create();
        var refreshToken = OAuthSecretGenerator.Create();
        var accessLifetime = TimeSpan.FromHours(1);
        var refreshLifetime = TimeSpan.FromDays(7);
        var accessExpiry = DateTime.UtcNow.Add(accessLifetime);

        await tokenStore.StoreAccessTokenAsync(
            accessToken,
            new TokenInfo
            {
                Username = session.Username,
                UserId = session.UserId,
                Roles = session.Roles,
                RefreshTokenHash = TokenHashing.Compute(refreshToken),
                Expiry = accessExpiry
            },
            accessLifetime);
        await tokenStore.StoreRefreshSessionAsync(
            refreshToken,
            session with { ExpiresAt = DateTime.UtcNow.Add(refreshLifetime) },
            refreshLifetime);

        return new OAuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "bearer",
            ExpiresIn = (int)accessLifetime.TotalSeconds,
            Username = session.Username,
            Roles = session.Roles,
            ExpiresAt = new DateTimeOffset(accessExpiry).ToString("o")
        };
    }
}
