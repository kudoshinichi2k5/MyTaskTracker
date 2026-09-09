using System.Net;
using System.Text;
using System.Text.Json;

namespace Tracker.TaskService.Tests;

public sealed class FakeAuthServiceVerifyHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = request.Headers.Authorization?.Parameter;

        if (string.IsNullOrWhiteSpace(token) || token == TestTokens.Invalid)
        {
            return Task.FromResult(Respond(active: false, username: null, roles: []));
        }

        var parts = token.Split(':', 2);
        var username = parts[0];
        var roles = parts.Length > 1 && parts[1].Length > 0
            ? parts[1].Split(',')
            : Array.Empty<string>();

        return Task.FromResult(Respond(active: true, username: username, roles: roles));
    }

    private static HttpResponseMessage Respond(bool active, string? username, string[] roles)
    {
        var json = JsonSerializer.Serialize(new { active, username, roles });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}

public static class TestTokens
{
    public const string Invalid = "invalid-token";

    public static string For(string username, params string[] roles) =>
        roles.Length == 0 ? username : $"{username}:{string.Join(',', roles)}";
}
