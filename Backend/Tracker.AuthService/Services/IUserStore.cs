using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Tracker.AuthService.Models;

namespace Tracker.AuthService.Services;

public interface IUserStore
{
    User? FindByUsername(string username);
    User? FindById(string id);
    bool UsernameExists(string username);
    User Create(User user);
    IReadOnlyList<User> GetAll();
    bool AddRole(string userId, string role);
    bool RemoveRole(string userId, string role);
}

/// <summary>
/// In-memory user store - same pattern as ITaskStore/INotificationStore
/// elsewhere in this solution: resets on every restart. Fine for dev/demo;
/// swap for a real database before this is anything more than that, since
/// losing every account on every deploy is not a real system's auth story.
///
/// Replaces the previous hardcoded `username == "admin" &amp;&amp; password ==
/// "123456"` check in Program.cs, which meant literally no other account
/// could ever exist - there was no way to test the "regular employee,
/// no admin role" path the whole Customer App / Admin Portal split assumes.
/// </summary>
public class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<string, User> _byId = new();

    public InMemoryUserStore(IPasswordHasher<User> hasher)
    {
        Seed(hasher);
    }

    private void Seed(IPasswordHasher<User> hasher)
    {
        // Dev-only seed accounts, kept as the same admin/123456 credential
        // that was hardcoded before (so nothing you already had running
        // locally breaks), plus one non-admin account so the "regular
        // employee" path is actually testable, which it wasn't before.
        // CHANGE both passwords (or delete these accounts) before this is
        // anything but a local sandbox.
        var admin = new User { Username = "admin", Email = "admin@tasktracker.local", PasswordHash = "", Roles = ["admin"] };
        admin.PasswordHash = hasher.HashPassword(admin, "123456");
        _byId[admin.Id] = admin;

        var employee = new User { Username = "alice", Email = "alice@tasktracker.local", PasswordHash = "", Roles = [] };
        employee.PasswordHash = hasher.HashPassword(employee, "employee123");
        _byId[employee.Id] = employee;
    }

    public User? FindByUsername(string username) =>
        _byId.Values.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

    public User? FindById(string id) => _byId.GetValueOrDefault(id);

    public bool UsernameExists(string username) => FindByUsername(username) is not null;

    public User Create(User user)
    {
        _byId[user.Id] = user;
        return user;
    }

    public IReadOnlyList<User> GetAll() => _byId.Values.OrderBy(u => u.Username).ToList();

    public bool AddRole(string userId, string role)
    {
        if (!_byId.TryGetValue(userId, out var user)) return false;
        if (!user.Roles.Contains(role)) user.Roles.Add(role);
        return true;
    }

    public bool RemoveRole(string userId, string role)
    {
        if (!_byId.TryGetValue(userId, out var user)) return false;
        user.Roles.Remove(role);
        return true;
    }
}