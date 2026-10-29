using Microsoft.EntityFrameworkCore;
using Tracker.TaskService.Data;
using Tracker.TaskService.Models;

namespace Tracker.TaskService.Services;

/// <summary>
/// EF Core / MariaDB-backed thay thế cho InMemoryTaskStore. Giữ nguyên hợp
/// đồng ITaskStore để Endpoints/TaskEndpoints.cs không phải sửa gì.
/// </summary>
public class EfTaskStore : ITaskStore
{
    private readonly TaskDbContext _db;

    public EfTaskStore(TaskDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<TaskItem> GetAll(string userId)
    {
        return GetAll(userId, null);
    }

    public IReadOnlyList<TaskItem> GetAll(string userId, Guid? projectId)
    {
        var query = _db.Tasks.Where(t => t.UserId == userId);
        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }
        return query.OrderBy(t => t.Id).ToList();
    }

    public TaskItem Create(string userId, string title)
    {
        return Create(userId, title, null, null);
    }

    public TaskItem Create(string userId, string title, bool? isCompleted, Guid? projectId)
    {
        var task = new TaskItem { UserId = userId, Title = title, IsCompleted = isCompleted ?? false, ProjectId = projectId };
        _db.Tasks.Add(task);
        _db.SaveChanges();
        return task;
    }

    public bool Update(string userId, int id, string? title, bool? isCompleted)
    {
        return Update(userId, id, title, isCompleted, null);
    }

    public bool Update(string userId, int id, string? title, bool? isCompleted, Guid? projectId)
    {
        var task = _db.Tasks.FirstOrDefault(t => t.Id == id && t.UserId == userId);
        if (task is null)
        {
            return false;
        }

        if (title is not null)
        {
            task.Title = title;
        }

        if (isCompleted is not null)
        {
            task.IsCompleted = isCompleted.Value;
        }

        if (projectId is not null)
        {
            task.ProjectId = projectId.Value;
        }

        _db.SaveChanges();
        return true;
    }

    public bool Delete(string userId, int id)
    {
        var task = _db.Tasks.FirstOrDefault(t => t.Id == id && t.UserId == userId);
        if (task is null)
        {
            return false;
        }

        _db.Tasks.Remove(task);
        _db.SaveChanges();
        return true;
    }

    public bool SetProjectId(int taskId, Guid? projectId)
    {
        var task = _db.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
        {
            return false;
        }

        task.ProjectId = projectId;
        _db.SaveChanges();
        return true;
    }

    public IReadOnlyList<UserTaskSummary> GetSummaryForAllUsers()
    {
        return _db.Tasks
            .AsEnumerable()
            .GroupBy(t => t.UserId)
            .Select(g => new UserTaskSummary(
                g.Key,
                g.Count(),
                g.Count(t => t.IsCompleted)))
            .OrderBy(s => s.UserId)
            .ToList();
    }
}