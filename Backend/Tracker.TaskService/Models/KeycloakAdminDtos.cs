namespace Tracker.TaskService.Models;

public record AuthUserDto
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool Enabled { get; init; }
}

public record AuthRoleDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record AdminUserSummary(string Id, string Username, string? Email, bool Enabled, List<string> Roles);
