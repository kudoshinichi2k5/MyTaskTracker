using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.ProjectService.Data;
using Tracker.ProjectService.Services;
using Xunit;

namespace Tracker.ProjectService.Tests;

public sealed class ProjectStoreTests
{
    private static ProjectStore CreateStore()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProjectStore(new ProjectDbContext(options));
    }

    [Fact]
    public void CreateAndList_ReturnsOnlyTheOwnersProjects()
    {
        var store = CreateStore();
        store.Create("alice", "Alice project", null);
        store.Create("bob", "Bob project", null);

        var projects = store.GetAllForUser("alice");

        projects.Should().ContainSingle().Which.OwnerUserId.Should().Be("alice");
    }

    [Fact]
    public void UpdateAndDelete_RequireTheOwner()
    {
        var store = CreateStore();
        var project = store.Create("alice", "Original", null);

        store.Update(project.Id, "bob", "Hijacked", null).Should().BeNull();
        store.Update(project.Id, "alice", "Updated", "Details")!.Name.Should().Be("Updated");
        store.Delete(project.Id, "bob").Should().BeFalse();
        store.Delete(project.Id, "alice").Should().BeTrue();
    }

    [Fact]
    public void AttachAndDetachTask_UpdatesTaskIds()
    {
        var store = CreateStore();
        var project = store.Create("alice", "Roadmap", null);

        store.AttachTask(project.Id, "alice", 42)!.TaskIds.Should().Contain(42);
        store.AttachTask(project.Id, "alice", 42)!.TaskIds.Should().ContainSingle();
        store.DetachTask(project.Id, "alice", 42)!.TaskIds.Should().BeEmpty();
    }
}
