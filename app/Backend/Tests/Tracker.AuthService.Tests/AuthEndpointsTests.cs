using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Tracker.AuthService.Tests;

public sealed class AuthEndpointsTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();
    private readonly HttpClient _client;

    public AuthEndpointsTests() => _client = _factory.CreateClient();

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
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task Login_WithSeededAdminCredentials_ReturnsAccessToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "123456" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Username.Should().Be("admin");
        body.Roles.Should().Contain("admin");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_WithPasswordGrant_ReturnsAccessToken()
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = "admin",
            ["password"] = "123456",
        });

        var response = await _client.PostAsync("/token", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body!.TokenType.Should().Be("bearer");
    }

    [Fact]
    public async Task Register_WithValidData_CreatesUserAndReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "newuser",
            email = "newuser@example.com",
            password = "correct-horse",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body!.Username.Should().Be("newuser");
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewTokenPair()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = "alice", password = "employee123" });
        var original = await login.Content.ReadFromJsonAsync<TokenResponse>();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original!.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<TokenResponse>();
        refreshed!.AccessToken.Should().NotBe(original.AccessToken);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = "alice", password = "employee123" });
        var original = await login.Content.ReadFromJsonAsync<TokenResponse>();

        var logout = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = original!.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.OK);

        var refresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Verify_WithFreshAccessToken_ReportsActive()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "123456" });
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/verify");
        request.Headers.Add("Authorization", $"Bearer {token!.AccessToken}");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VerifyResponse>();
        body!.Active.Should().BeTrue();
        body.Username.Should().Be("admin");
    }

    [Fact]
    public async Task Verify_WithoutAuthorizationHeader_ReportsInactive()
    {
        var response = await _client.GetAsync("/verify");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VerifyResponse>();
        body!.Active.Should().BeFalse();
    }

    private sealed record HealthResponse(string Status);
    private sealed record TokenResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn, string Username, string[] Roles, string ExpiresAt);
    private sealed record VerifyResponse(bool Active, string? Username, string[] Roles);
}
