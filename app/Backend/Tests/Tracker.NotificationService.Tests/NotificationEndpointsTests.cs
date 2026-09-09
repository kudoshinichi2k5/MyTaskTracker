using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tracker.NotificationService.Data;
using Tracker.NotificationService.Models;
using Xunit;

namespace Tracker.NotificationService.Tests;

public sealed class NotificationEndpointsTests : IDisposable
{
    private readonly NotificationApiFactory _factory = new();
    private readonly HttpClient _client;

    public NotificationEndpointsTests() => _client = _factory.CreateClient();

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
    public async Task Notifications_RequireAuthentication()
    {
        var response = await _client.GetAsync("/api/v1/notifications/");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserCanReadOwnUnreadCountAndMarkNotificationRead()
    {
        int notificationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var item = new NotificationItem { UserId = "alice", Message = "Build finished", IsRead = false };
            db.Notifications.Add(item);
            db.SaveChanges();
            notificationId = item.Id;
        }

        using var countRequest = Authenticated(HttpMethod.Get, "/api/v1/notifications/unread-count", "alice");
        var count = await _client.SendAsync(countRequest);
        count.StatusCode.Should().Be(HttpStatusCode.OK);
        (await count.Content.ReadFromJsonAsync<CountResponse>())!.Count.Should().Be(1);

        using var readRequest = Authenticated(HttpMethod.Post, $"/api/v1/notifications/{notificationId}/read", "alice");
        var read = await _client.SendAsync(readRequest);
        read.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UserCannotMarkAnotherUsersNotificationRead()
    {
        int notificationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var item = new NotificationItem { UserId = "alice", Message = "Private", IsRead = false };
            db.Notifications.Add(item);
            db.SaveChanges();
            notificationId = item.Id;
        }

        using var request = Authenticated(HttpMethod.Post, $"/api/v1/notifications/{notificationId}/read", "bob");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static HttpRequestMessage Authenticated(HttpMethod method, string url, string user)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new("Bearer", user);
        return request;
    }

    private sealed record CountResponse(int Count);
}
