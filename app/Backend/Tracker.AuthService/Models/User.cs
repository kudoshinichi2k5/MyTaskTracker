namespace Tracker.AuthService.Models;

public class User
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool Enabled { get; set; } = true;
}