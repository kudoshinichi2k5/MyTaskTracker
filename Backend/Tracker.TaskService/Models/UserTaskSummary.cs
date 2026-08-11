namespace Tracker.TaskService.Models;

public record AdminUserSummary(string Id, string Username, string? Email, bool Enabled, List<string> Roles);

// One row of the admin "Reports" screen: how many tasks a given user has,
// and how many are done. UserId is the authenticated JWT "sub" claim; the
// Admin Portal can map it to a friendly name/email if it wants to do so.
public record UserTaskSummary(string UserId, int TotalTasks, int CompletedTasks);