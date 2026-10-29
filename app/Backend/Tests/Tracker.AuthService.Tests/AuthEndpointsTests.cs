using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace Tracker.AuthService.Tests;

public sealed class AuthEndpointsTests : IDisposable
{
    private const string ClientId = "mytasktracker-customer";
    private const string RedirectUri = "http://localhost:4200/oauth/callback";
    private const string Verifier = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._~abc";
    private readonly AuthApiFactory _factory = new();
    private readonly HttpClient _client;

    public AuthEndpointsTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthorizationCode_WithPkce_ReturnsOpaqueTokenPair()
    {
        var code = await AuthorizeAsync("admin", "123456");
        var response = await ExchangeCodeAsync(code, Verifier);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.AccessToken.Should().NotContain(".");
        body.Username.Should().Be("admin");
        body.Roles.Should().Contain("admin");
    }

    [Fact]
    public async Task AuthorizationCode_CannotBeReplayed()
    {
        var code = await AuthorizeAsync("alice", "employee123");
        (await ExchangeCodeAsync(code, Verifier)).StatusCode.Should().Be(HttpStatusCode.OK);

        var replay = await ExchangeCodeAsync(code, Verifier);
        replay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AuthorizationCode_WithWrongVerifier_IsRejected()
    {
        var code = await AuthorizeAsync("alice", "employee123");
        var response = await ExchangeCodeAsync(code, Verifier + "x");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PasswordGrant_IsNotSupported()
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = ClientId,
            ["username"] = "admin",
            ["password"] = "123456"
        });
        var response = await _client.PostAsync("/connect/token", form);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_RotatesAndReplayFails()
    {
        var code = await AuthorizeAsync("alice", "employee123");
        var tokenResponse = await ExchangeCodeAsync(code, Verifier);
        var original = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["refresh_token"] = original!.RefreshToken
        });
        var refreshed = await _client.PostAsync("/connect/token", form);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

        using var replayForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["refresh_token"] = original.RefreshToken
        });
        var replay = await _client.PostAsync("/connect/token", replayForm);
        replay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_CreatesAccountWithoutIssuingTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "newuser",
            email = "newuser@example.com",
            password = "correct-horse"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("newuser").And.NotContain("accessToken");
    }

    private async Task<string> AuthorizeAsync(string username, string password)
    {
        var authorizeUrl = QueryHelpers.AddQueryString("/connect/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = ClientId,
            ["redirect_uri"] = RedirectUri,
            ["scope"] = "api",
            ["state"] = "test-state",
            ["code_challenge"] = ChallengeFor(Verifier),
            ["code_challenge_method"] = "S256"
        });
        var authorize = await _client.GetAsync(authorizeUrl);
        authorize.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await authorize.Content.ReadAsStringAsync();
        var transaction = Regex.Match(html, "name=\\\"transaction\\\" value=\\\"([^\\\"]+)\\\"").Groups[1].Value;
        transaction.Should().NotBeNullOrWhiteSpace();
        var cookie = authorize.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/authorize")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["transaction"] = transaction,
                ["username"] = username,
                ["password"] = password
            })
        };
        request.Headers.Add("Cookie", cookie);
        var completed = await _client.SendAsync(request);
        completed.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return QueryHelpers.ParseQuery(completed.Headers.Location!.Query)["code"].Single()!;
    }

    private async Task<HttpResponseMessage> ExchangeCodeAsync(string code, string verifier)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["code"] = code,
            ["redirect_uri"] = RedirectUri,
            ["code_verifier"] = verifier
        });
        return await _client.PostAsync("/connect/token", form);
    }

    private static string ChallengeFor(string verifier) =>
        WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private sealed record TokenResponse(string AccessToken, string RefreshToken, string Username, string[] Roles);
}
