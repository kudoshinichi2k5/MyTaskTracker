using Microsoft.EntityFrameworkCore;
using Tracker.CommentService.Data;
using Tracker.CommentService.Models;

namespace Tracker.CommentService.Services;

/// <summary>
/// EF Core / MariaDB-backed comment store. Thay bản in-memory cũ, giữ
/// nguyên tên class + API để CommentEndpoints.cs không phải sửa gì.
/// </summary>
public sealed class CommentStore
{
    private readonly CommentDbContext _db;

    public CommentStore(CommentDbContext db)
    {
        _db = db;
    }

    public IReadOnlyCollection<CommentItem> GetForTask(int taskId)
    {
        return _db.Comments
            .AsNoTracking()
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToList();
    }

    public CommentItem Add(int taskId, string authorUserId, string body)
    {
        var comment = new CommentItem
        {
            TaskId = taskId,
            AuthorUserId = authorUserId,
            Body = body
        };

        _db.Comments.Add(comment);
        _db.SaveChanges();

        return comment;
    }

    public CommentItem? GetById(Guid id)
    {
        return _db.Comments.AsNoTracking().FirstOrDefault(c => c.Id == id);
    }

    public CommentItem? Update(Guid id, string authorUserId, string body)
    {
        var comment = _db.Comments.FirstOrDefault(c => c.Id == id && c.AuthorUserId == authorUserId);
        if (comment is null)
        {
            return null;
        }

        comment.Body = body;
        comment.EditedAt = DateTimeOffset.UtcNow;
        _db.SaveChanges();

        return comment;
    }

    public bool Delete(Guid id, string authorUserId)
    {
        var comment = _db.Comments.FirstOrDefault(c => c.Id == id && c.AuthorUserId == authorUserId);
        if (comment is null)
        {
            return false;
        }

        _db.Comments.Remove(comment);
        _db.SaveChanges();

        return true;
    }
}