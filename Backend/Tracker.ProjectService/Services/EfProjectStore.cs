using Microsoft.EntityFrameworkCore;
using Tracker.ProjectService.Data;
using Tracker.ProjectService.Models;

namespace Tracker.ProjectService.Services;

public sealed class EfProjectStore
{
    private readonly ProjectDbContext _db;

    public EfProjectStore(ProjectDbContext db)
    {
        _db = db;
    }

    public ProjectItem Create(
        string ownerUserId,
        string name,
        string? description)
    {
        var project = new ProjectItem
        {
            Name = name,
            Description = description,
            OwnerUserId = ownerUserId
        };

        _db.Projects.Add(project);
        _db.SaveChanges();

        return project;
    }

    public IReadOnlyCollection<ProjectItem>
        GetAllForUser(string userId)
    {
        return _db.Projects
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ToList();
    }

    public ProjectItem? GetById(Guid id)
    {
        return _db.Projects
            .AsNoTracking()
            .FirstOrDefault(
                p => p.Id == id);
    }

    public ProjectItem? Update(
        Guid id,
        string ownerUserId,
        string name,
        string? description)
    {
        var project =
            _db.Projects.FirstOrDefault(
                p => p.Id == id &&
                     p.OwnerUserId == ownerUserId);

        if (project is null)
        {
            return null;
        }

        project.Name = name;
        project.Description = description;
        project.UpdatedAt =
            DateTimeOffset.UtcNow;

        _db.SaveChanges();

        return project;
    }

    public bool Delete(
        Guid id,
        string ownerUserId)
    {
        var project =
            _db.Projects.FirstOrDefault(
                p => p.Id == id &&
                     p.OwnerUserId == ownerUserId);

        if (project is null)
        {
            return false;
        }

        _db.Projects.Remove(project);
        _db.SaveChanges();

        return true;
    }

    public ProjectItem? AttachTask(
        Guid projectId,
        string ownerUserId,
        int taskId)
    {
        var project =
            _db.Projects.FirstOrDefault(
                p => p.Id == projectId &&
                     p.OwnerUserId == ownerUserId);

        if (project is null)
        {
            return null;
        }

        if (!project.TaskIds.Contains(taskId))
        {
            project.TaskIds.Add(taskId);
            project.UpdatedAt =
                DateTimeOffset.UtcNow;

            _db.SaveChanges();
        }

        return project;
    }

    public ProjectItem? DetachTask(
        Guid projectId,
        string ownerUserId,
        int taskId)
    {
        var project =
            _db.Projects.FirstOrDefault(
                p => p.Id == projectId &&
                     p.OwnerUserId == ownerUserId);

        if (project is null)
        {
            return null;
        }

        project.TaskIds.Remove(taskId);
        project.UpdatedAt =
            DateTimeOffset.UtcNow;

        _db.SaveChanges();

        return project;
    }
}