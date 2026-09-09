using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.TaskService.Data;
using Tracker.TaskService.Services;
using Xunit;

namespace Tracker.TaskService.Tests;

public sealed class EfTaskStoreTests
{
    private static EfTaskStore CreateStore()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EfTaskStore(new TaskDbContext(options));
    }

    [Fact]
    public void Create_PersistsTask_ScopedToTheGivenUser()
    {
        var store = CreateStore();

        var task = store.Create("alice", "Buy milk");

        task.Id.Should().BeGreaterThan(0);
        task.UserId.Should().Be("alice");
        task.Title.Should().Be("Buy milk");
        task.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void GetAll_OnlyReturnsTasksForTheRequestedUser()
    {
        var store = CreateStore();
        store.Create("alice", "Alice task 1");
        store.Create("alice", "Alice task 2");
        store.Create("bob", "Bob task");

        var aliceTasks = store.GetAll("alice");

        aliceTasks.Should().HaveCount(2);
        aliceTasks.Should().OnlyContain(t => t.UserId == "alice");
    }

    [Fact]
    public void GetAll_ReturnsEmptyList_ForUnknownUser()
    {
        var store = CreateStore();

        store.GetAll("nobody").Should().BeEmpty();
    }

    [Fact]
    public void Update_ChangesTitleAndCompletionState()
    {
        var store = CreateStore();
        var task = store.Create("alice", "Original title");

        var updated = store.Update("alice", task.Id, "New title", true);

        updated.Should().BeTrue();
        var stored = store.GetAll("alice").Single();
        stored.Title.Should().Be("New title");
        stored.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Update_WithNullFields_LeavesThemUnchanged()
    {
        var store = CreateStore();
        var task = store.Create("alice", "Keep me");

        store.Update("alice", task.Id, title: null, isCompleted: true);

        var stored = store.GetAll("alice").Single();
        stored.Title.Should().Be("Keep me");
        stored.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Update_ReturnsFalse_WhenTaskBelongsToAnotherUser()
    {
        var store = CreateStore();
        var task = store.Create("alice", "Alice's task");

        var updated = store.Update("bob", task.Id, "Hijacked title", null);

        updated.Should().BeFalse();
        store.GetAll("alice").Single().Title.Should().Be("Alice's task");
    }

    [Fact]
    public void Update_ReturnsFalse_WhenTaskDoesNotExist()
    {
        var store = CreateStore();

        store.Update("alice", 999999, "Nope", null).Should().BeFalse();
    }

    [Fact]
    public void Delete_RemovesOnlyTheOwnersTask()
    {
        var store = CreateStore();
        var task = store.Create("alice", "Delete me");

        var deleted = store.Delete("alice", task.Id);

        deleted.Should().BeTrue();
        store.GetAll("alice").Should().BeEmpty();
    }

    [Fact]
    public void Delete_ReturnsFalse_WhenTaskBelongsToAnotherUser()
    {
        var store = CreateStore();
        var task = store.Create("alice", "Protected task");

        var deleted = store.Delete("bob", task.Id);

        deleted.Should().BeFalse();
        store.GetAll("alice").Should().ContainSingle();
    }

    [Fact]
    public void GetSummaryForAllUsers_AggregatesCountsPerUser()
    {
        var store = CreateStore();
        var t1 = store.Create("alice", "Task 1");
        store.Create("alice", "Task 2");
        store.Create("bob", "Task 3");
        store.Update("alice", t1.Id, null, true);

        var summary = store.GetSummaryForAllUsers();

        summary.Should().Contain(s => s.UserId == "alice" && s.TotalTasks == 2 && s.CompletedTasks == 1);
        summary.Should().Contain(s => s.UserId == "bob" && s.TotalTasks == 1 && s.CompletedTasks == 0);
    }
}
