namespace Tracker.TaskService.Models;

// One row of the admin "Reports" screen: how many tasks a given user has,
// and how many are done. UserId is the authenticated JWT "sub" claim; the
// Admin Portal can map it to a friendly name/email if it wants to do so.
public record UserTaskSummary(string UserId, int TotalTasks, int CompletedTasks);