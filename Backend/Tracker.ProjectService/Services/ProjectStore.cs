using System.Collections.Concurrent;
using Tracker.ProjectService.Models;

namespace Tracker.ProjectService.Services;

/// <summary>
/// In-memory project store, mirroring the "no database persistence yet"
/// limitation documented for the other services in this repo.
/// </summary>
public sealed class ProjectStore
{
    private readonly ConcurrentDictionary<Guid, ProjectItem> _projects = new();

    public ProjectItem Create(string ownerUserId, string name, string? description)
    {
        var project = new ProjectItem
        {
            Name = name,
            Description = description,
            OwnerUserId = ownerUserId
        };

        _projects[project.Id] = project;
        return project;
    }

    public IReadOnlyCollection<ProjectItem> GetAllForUser(string userId) =>
        _projects.Values.Where(p => p.OwnerUserId == userId).ToList();

    public ProjectItem? GetById(Guid id) =>
        _projects.TryGetValue(id, out var project) ? project : null;

    public ProjectItem? Update(Guid id, string ownerUserId, string name, string? description)
    {
        if (!_projects.TryGetValue(id, out var project) || project.OwnerUserId != ownerUserId)
        {
            return null;
        }

        project.Name = name;
        project.Description = description;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        return project;
    }

    public bool Delete(Guid id, string ownerUserId)
    {
        if (!_projects.TryGetValue(id, out var project) || project.OwnerUserId != ownerUserId)
        {
            return false;
        }

        return _projects.TryRemove(id, out _);
    }

    public ProjectItem? AttachTask(Guid projectId, string ownerUserId, Guid taskId)
    {
        if (!_projects.TryGetValue(projectId, out var project) || project.OwnerUserId != ownerUserId)
        {
            return null;
        }

        if (!project.TaskIds.Contains(taskId))
        {
            project.TaskIds.Add(taskId);
            project.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return project;
    }

    public ProjectItem? DetachTask(Guid projectId, string ownerUserId, Guid taskId)
    {
        if (!_projects.TryGetValue(projectId, out var project) || project.OwnerUserId != ownerUserId)
        {
            return null;
        }

        if (project.TaskIds.Remove(taskId))
        {
            project.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return project;
    }
}
