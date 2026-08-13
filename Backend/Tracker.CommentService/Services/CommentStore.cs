using System.Collections.Concurrent;
using Tracker.CommentService.Models;

namespace Tracker.CommentService.Services;

/// <summary>
/// In-memory comment store, mirroring the "no database persistence yet"
/// limitation documented for the other services in this repo.
/// </summary>
public sealed class CommentStore
{
    private readonly ConcurrentDictionary<Guid, CommentItem> _comments = new();

    public CommentItem Add(Guid taskId, string authorUserId, string body)
    {
        var comment = new CommentItem
        {
            TaskId = taskId,
            AuthorUserId = authorUserId,
            Body = body
        };

        _comments[comment.Id] = comment;
        return comment;
    }

    public IReadOnlyCollection<CommentItem> GetForTask(Guid taskId) =>
        _comments.Values
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToList();

    public CommentItem? GetById(Guid id) =>
        _comments.TryGetValue(id, out var comment) ? comment : null;

    public CommentItem? Update(Guid id, string authorUserId, string body)
    {
        if (!_comments.TryGetValue(id, out var comment) || comment.AuthorUserId != authorUserId)
        {
            return null;
        }

        comment.Body = body;
        comment.EditedAt = DateTimeOffset.UtcNow;
        return comment;
    }

    public bool Delete(Guid id, string authorUserId)
    {
        if (!_comments.TryGetValue(id, out var comment) || comment.AuthorUserId != authorUserId)
        {
            return false;
        }

        return _comments.TryRemove(id, out _);
    }
}
