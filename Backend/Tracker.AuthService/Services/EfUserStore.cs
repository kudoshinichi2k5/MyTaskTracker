using Microsoft.EntityFrameworkCore;
using Tracker.AuthService.Data;
using Tracker.AuthService.Models;

namespace Tracker.AuthService.Services;

public class EfUserStore : IUserStore
{
    private readonly AuthDbContext _db;

    public EfUserStore(AuthDbContext db)
    {
        _db = db;
    }

    public User? FindByUsername(string username)
    {
        return _db.Users
            .FirstOrDefault(u => u.Username == username);
    }

    public User? FindById(string id)
    {
        return _db.Users
            .FirstOrDefault(u => u.Id == id);
    }

    public bool UsernameExists(string username)
    {
        return _db.Users
            .Any(u => u.Username == username);
    }

    public bool EmailExists(string email)
    {
        return _db.Users
            .Any(u => u.Email == email);
    }

    public User Create(User user)
    {
        _db.Users.Add(user);
        _db.SaveChanges();

        return user;
    }

    public IReadOnlyList<User> GetAll()
    {
        return _db.Users
            .OrderBy(u => u.Username)
            .ToList();
    }

    public bool AddRole(string userId, string role)
    {
        var user = _db.Users
            .FirstOrDefault(u => u.Id == userId);

        if (user is null)
        {
            return false;
        }

        if (!user.Roles.Contains(role))
        {
            user.Roles.Add(role);
            _db.SaveChanges();
        }

        return true;
    }

    public bool RemoveRole(string userId, string role)
    {
        var user = _db.Users
            .FirstOrDefault(u => u.Id == userId);

        if (user is null)
        {
            return false;
        }

        user.Roles.Remove(role);
        _db.SaveChanges();

        return true;
    }
}