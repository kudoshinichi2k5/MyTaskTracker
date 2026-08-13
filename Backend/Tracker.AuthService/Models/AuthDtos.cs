namespace Tracker.AuthService.Models;

public record RegisterRequest(string Username, string Email, string Password);

// Clean shape for the Admin Portal's Users screen - never expose PasswordHash.
public record AdminUserSummary(string Id, string Username, string Email, bool Enabled, List<string> Roles);