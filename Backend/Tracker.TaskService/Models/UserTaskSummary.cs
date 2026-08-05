namespace Tracker.TaskService.Models;

// One row of the admin "Reports" screen: how many tasks a given user has,
// and how many are done. UserId is the Keycloak "sub" claim - the Admin
// Portal is responsible for mapping that to a friendly name/email if it
// wants one (via Keycloak's own user info, not something TaskService knows).
public record UserTaskSummary(string UserId, int TotalTasks, int CompletedTasks);