using Tracker.TaskService.Models;

namespace Tracker.TaskService.Services;

public interface ITaskStore
{
    IReadOnlyList<TaskItem> GetAll(string userId);
    IReadOnlyList<TaskItem> GetAll(string userId, Guid? projectId);
    TaskItem Create(string userId, string title);
    TaskItem Create(string userId, string title, bool? isCompleted, Guid? projectId);
    bool Update(string userId, int id, string? title, bool? isCompleted, Guid? projectId);
    bool Delete(string userId, int id);
    bool SetProjectId(int taskId, Guid? projectId);

    // Powers the Admin Portal's Reports screen. Only includes users who have
    // hit this service at least once (i.e. signed into the Customer App) -
    // it's an in-memory approximation, not a full external identity directory.
    IReadOnlyList<UserTaskSummary> GetSummaryForAllUsers();
}