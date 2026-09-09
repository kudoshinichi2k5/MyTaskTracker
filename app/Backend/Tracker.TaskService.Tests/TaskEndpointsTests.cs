using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Tracker.TaskService.Tests;

public sealed class TaskEndpointsTests : IDisposable
{
    private readonly TaskApiFactory _factory = new();
    private readonly HttpClient _client;

    public TaskEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetTasks_WithoutAuthorizationHeader_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTask_ThenGetTasks_ReturnsTheCreatedTask()
    {
        using var createRequest = _factory.AuthenticatedRequest(HttpMethod.Post, "/api/v1/tasks", "alice");
        createRequest.Content = JsonContent.Create(new { title = "Write tests" });
        var createResponse = await _client.SendAsync(createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskItemResponse>();
        created!.Title.Should().Be("Write tests");

        using var listRequest = _factory.AuthenticatedRequest(HttpMethod.Get, "/api/v1/tasks", "alice");
        var listResponse = await _client.SendAsync(listRequest);
        var tasks = await listResponse.Content.ReadFromJsonAsync<List<TaskItemResponse>>();

        tasks.Should().ContainSingle(t => t.Id == created.Id && t.Title == "Write tests");
    }

    [Fact]
    public async Task CreateTask_WithBlankTitle_ReturnsBadRequest()
    {
        using var request = _factory.AuthenticatedRequest(HttpMethod.Post, "/api/v1/tasks", "alice");
        request.Content = JsonContent.Create(new { title = "   " });

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTasks_OnlyReturnsTasksBelongingToTheCallingUser()
    {
        using var aliceCreate = _factory.AuthenticatedRequest(HttpMethod.Post, "/api/v1/tasks", "alice");
        aliceCreate.Content = JsonContent.Create(new { title = "Alice's task" });
        await _client.SendAsync(aliceCreate);

        using var bobCreate = _factory.AuthenticatedRequest(HttpMethod.Post, "/api/v1/tasks", "bob");
        bobCreate.Content = JsonContent.Create(new { title = "Bob's task" });
        await _client.SendAsync(bobCreate);

        using var bobList = _factory.AuthenticatedRequest(HttpMethod.Get, "/api/v1/tasks", "bob");
        var response = await _client.SendAsync(bobList);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskItemResponse>>();

        tasks.Should().ContainSingle().Which.Title.Should().Be("Bob's task");
    }

    [Fact]
    public async Task UpdateTask_MarksItCompleted()
    {
        using var createRequest = _factory.AuthenticatedRequest(HttpMethod.Post, "/api/v1/tasks", "alice");
        createRequest.Content = JsonContent.Create(new { title = "Ship the feature" });
        var created = await (await _client.SendAsync(createRequest)).Content.ReadFromJsonAsync<TaskItemResponse>();

        using var updateRequest = _factory.AuthenticatedRequest(HttpMethod.Put, $"/api/v1/tasks/{created!.Id}", "alice");
        updateRequest.Content = JsonContent.Create(new { isCompleted = true });
        var updateResponse = await _client.SendAsync(updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTask_RemovesIt()
    {
        using var createRequest = _factory.AuthenticatedRequest(HttpMethod.Post, "/api/v1/tasks", "alice");
        createRequest.Content = JsonContent.Create(new { title = "Temporary task" });
        var created = await (await _client.SendAsync(createRequest)).Content.ReadFromJsonAsync<TaskItemResponse>();

        using var deleteRequest = _factory.AuthenticatedRequest(HttpMethod.Delete, $"/api/v1/tasks/{created!.Id}", "alice");
        var deleteResponse = await _client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AdminSummary_WithoutAdminRole_ReturnsForbidden()
    {
        using var request = _factory.AuthenticatedRequest(HttpMethod.Get, "/api/v1/tasks/admin/summary", "alice");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminSummary_WithAdminRole_AggregatesAcrossUsers()
    {
        using var aliceCreate = _factory.AuthenticatedRequest(HttpMethod.Post, "/api/v1/tasks", "alice");
        aliceCreate.Content = JsonContent.Create(new { title = "Task 1" });
        await _client.SendAsync(aliceCreate);

        using var summaryRequest = _factory.AuthenticatedRequest(HttpMethod.Get, "/api/v1/tasks/admin/summary", "admin", "admin");
        var response = await _client.SendAsync(summaryRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.Should().Be("healthy");
    }

    private sealed record TaskItemResponse(int Id, string UserId, string Title, bool IsCompleted);
    private sealed record HealthResponse(string Status, string Environment);
}
