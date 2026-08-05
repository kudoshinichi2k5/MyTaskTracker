namespace Tracker.TaskService.Models;

public record KeycloakUserDto
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool Enabled { get; init; }
}

public record KeycloakRoleDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record AdminUserSummary(string Id, string Username, string? Email, bool Enabled, List<string> Roles);
