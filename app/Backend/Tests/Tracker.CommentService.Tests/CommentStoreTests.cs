using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.CommentService.Data;
using Tracker.CommentService.Services;
using Xunit;

namespace Tracker.CommentService.Tests;

public sealed class CommentStoreTests
{
    private static CommentStore CreateStore()
    {
        var options = new DbContextOptionsBuilder<CommentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CommentStore(new CommentDbContext(options));
    }

    [Fact]
    public void AddAndList_ReturnsCommentsForTaskInCreationOrder()
    {
        var store = CreateStore();
        store.Add(42, "alice", "First");
        store.Add(42, "bob", "Second");
        store.Add(7, "alice", "Other task");

        var comments = store.GetForTask(42);

        comments.Should().HaveCount(2);
        comments.Select(c => c.Body).Should().ContainInOrder("First", "Second");
    }

    [Fact]
    public void UpdateAndDelete_RequireTheAuthor()
    {
        var store = CreateStore();
        var comment = store.Add(42, "alice", "Original");

        store.Update(comment.Id, "bob", "Hijacked").Should().BeNull();
        store.Update(comment.Id, "alice", "Edited")!.Body.Should().Be("Edited");
        store.Delete(comment.Id, "bob").Should().BeFalse();
        store.Delete(comment.Id, "alice").Should().BeTrue();
    }
}
