using System.Net;
using System.Text;
using System.Text.Json;

namespace Tracker.NotificationService.Tests;

public sealed class FakeAuthServiceVerifyHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = request.Headers.Authorization?.Parameter;
        var active = !string.IsNullOrWhiteSpace(token) && token != "invalid-token";
        var json = JsonSerializer.Serialize(new { active, username = active ? token : null, roles = Array.Empty<string>() });
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
