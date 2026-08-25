using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tracker.AuthService.Models;

namespace Tracker.AuthService.Data;

public static class AuthDbSeeder
{
    public static void Seed(
        AuthDbContext db,
        IPasswordHasher<User> hasher)
    {
        if (db.Users.Any())
        {
            return;
        }

        var admin = new User
        {
            Username = "admin",
            Email = "admin@tasktracker.local",
            PasswordHash = "",
            Roles = ["admin"],
            Enabled = true
        };

        admin.PasswordHash =
            hasher.HashPassword(
                admin,
                "123456");

        var employee = new User
        {
            Username = "alice",
            Email = "alice@tasktracker.local",
            PasswordHash = "",
            Roles = [],
            Enabled = true
        };

        employee.PasswordHash =
            hasher.HashPassword(
                employee,
                "employee123");

        db.Users.AddRange(
            admin,
            employee);

        db.SaveChanges();
    }
}