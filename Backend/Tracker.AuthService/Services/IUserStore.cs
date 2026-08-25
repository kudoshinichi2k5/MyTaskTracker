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