namespace Tracker.CommentService.Models;

public sealed class CommentItem
{
    public Guid Id { get; init; }
        = Guid.NewGuid();

    // Must match Tracker.TaskService.TaskItem.Id.
    public required int TaskId { get; init; }

    public required string AuthorUserId { get; init; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? EditedAt { get; set; }
}

public sealed record CreateCommentRequest(
    string Body);

public sealed record UpdateCommentRequest(
    string Body);

public sealed record CommentResponse(
    Guid Id,
    int TaskId,
    string AuthorUserId,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt)
{
    public static CommentResponse FromEntity(
        CommentItem entity) =>
        new(
            entity.Id,
            entity.TaskId,
            entity.AuthorUserId,
            entity.Body,
            entity.CreatedAt,
            entity.EditedAt);
}