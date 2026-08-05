using Tracker.TaskService.Models;

namespace Tracker.TaskService.Services;

public interface IKeycloakAdminClient
{
    Task<List<AdminUserSummary>> GetUsersWithRolesAsync(CancellationToken ct = default);
    Task<List<string>> GetRealmRolesAsync(CancellationToken ct = default);
    Task AssignRealmRoleAsync(string userId, string roleName, CancellationToken ct = default);
    Task RemoveRealmRoleAsync(string userId, string roleName, CancellationToken ct = default);
}

public class KeycloakAdminException(string message) : Exception(message);
