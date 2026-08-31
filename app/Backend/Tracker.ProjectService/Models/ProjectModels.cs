namespace Tracker.ProjectService.Models;

public sealed class ProjectItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required string OwnerUserId { get; set; }

    public List<int> TaskIds { get; set; } = [];

    public DateTimeOffset CreatedAt { get; init; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}

public sealed record CreateProjectRequest(
    string Name,
    string? Description);

public sealed record UpdateProjectRequest(
    string Name,
    string? Description);

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    string OwnerUserId,
    IReadOnlyList<int> TaskIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProjectResponse FromEntity(
        ProjectItem entity)
    {
        return new ProjectResponse(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.OwnerUserId,
            entity.TaskIds,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}