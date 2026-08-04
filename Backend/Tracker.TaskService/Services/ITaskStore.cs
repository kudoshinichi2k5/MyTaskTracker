using Tracker.TaskService.Models;

namespace Tracker.TaskService.Services;

public interface ITaskStore
{
    IReadOnlyList<TaskItem> GetAll(string userId);
    TaskItem Create(string userId, string title);
    bool Update(string userId, int id, string? title, bool? isCompleted);
    bool Delete(string userId, int id);
}