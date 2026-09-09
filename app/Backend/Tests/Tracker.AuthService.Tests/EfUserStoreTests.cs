using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tracker.AuthService.Data;
using Tracker.AuthService.Models;
using Tracker.AuthService.Services;
using Xunit;

namespace Tracker.AuthService.Tests;

public sealed class EfUserStoreTests
{
    private static EfUserStore CreateStore(out AuthDbContext context)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AuthDbContext(options);
        return new EfUserStore(context);
    }

    private static User NewUser(string username = "jdoe", string email = "jdoe@example.com") => new()
    {
        Username = username,
        Email = email,
        PasswordHash = "hashed-password",
    };

    [Fact]
    public void Create_PersistsUser_AndReturnsIt()
    {
        var store = CreateStore(out _);
        var user = NewUser();

        var created = store.Create(user);

        created.Should().BeSameAs(user);
        store.FindByUsername("jdoe").Should().NotBeNull();
    }

    [Fact]
    public void FindByUsername_ReturnsNull_WhenUserDoesNotExist()
    {
        var store = CreateStore(out _);

        store.FindByUsername("ghost").Should().BeNull();
    }

    [Fact]
    public void FindById_ReturnsMatchingUser()
    {
        var store = CreateStore(out _);
        var user = store.Create(NewUser());

        var found = store.FindById(user.Id);

        found.Should().NotBeNull();
        found!.Username.Should().Be("jdoe");
    }

    [Theory]
    [InlineData("jdoe", true)]
    [InlineData("nobody", false)]
    public void UsernameExists_ReflectsStoredUsers(string username, bool expected)
    {
        var store = CreateStore(out _);
        store.Create(NewUser());

        store.UsernameExists(username).Should().Be(expected);
    }

    [Theory]
    [InlineData("jdoe@example.com", true)]
    [InlineData("nobody@example.com", false)]
    public void EmailExists_ReflectsStoredUsers(string email, bool expected)
    {
        var store = CreateStore(out _);
        store.Create(NewUser());

        store.EmailExists(email).Should().Be(expected);
    }

    [Fact]
    public void GetAll_ReturnsUsersOrderedByUsername()
    {
        var store = CreateStore(out _);
        store.Create(NewUser("carol", "carol@example.com"));
        store.Create(NewUser("alice", "alice@example.com"));
        store.Create(NewUser("bob", "bob@example.com"));

        var all = store.GetAll();

        all.Select(u => u.Username).Should().ContainInOrder("alice", "bob", "carol");
    }

    [Fact]
    public void AddRole_AddsRole_WhenNotAlreadyPresent()
    {
        var store = CreateStore(out _);
        var user = store.Create(NewUser());

        var result = store.AddRole(user.Id, "manager");

        result.Should().BeTrue();
        store.FindById(user.Id)!.Roles.Should().Contain("manager");
    }

    [Fact]
    public void AddRole_IsIdempotent_WhenRoleAlreadyPresent()
    {
        var store = CreateStore(out _);
        var user = store.Create(NewUser());
        store.AddRole(user.Id, "manager");

        store.AddRole(user.Id, "manager");

        store.FindById(user.Id)!.Roles.Should().ContainSingle(role => role == "manager");
    }

    [Fact]
    public void AddRole_ReturnsFalse_WhenUserDoesNotExist()
    {
        var store = CreateStore(out _);

        store.AddRole("missing-user-id", "manager").Should().BeFalse();
    }

    [Fact]
    public void RemoveRole_RemovesRole_WhenPresent()
    {
        var store = CreateStore(out _);
        var user = store.Create(NewUser());
        store.AddRole(user.Id, "manager");

        var result = store.RemoveRole(user.Id, "manager");

        result.Should().BeTrue();
        store.FindById(user.Id)!.Roles.Should().NotContain("manager");
    }

    [Fact]
    public void RemoveRole_ReturnsFalse_WhenUserDoesNotExist()
    {
        var store = CreateStore(out _);

        store.RemoveRole("missing-user-id", "manager").Should().BeFalse();
    }

    [Fact]
    public void RemoveRole_ReturnsTrue_EvenWhenRoleWasNeverPresent()
    {
        var store = CreateStore(out _);
        var user = store.Create(NewUser());

        store.RemoveRole(user.Id, "never-had-this-role").Should().BeTrue();
    }
}
