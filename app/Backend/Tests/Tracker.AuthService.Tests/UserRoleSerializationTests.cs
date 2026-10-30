using Tracker.AuthService.Data;
using Xunit;

namespace Tracker.AuthService.Tests;

public sealed class UserRoleSerializationTests
{
    [Theory]
    [InlineData("[\"admin\"]", "admin")]
    [InlineData("admin", "admin")]
    [InlineData("admin, manager", "admin", "manager")]
    public void Deserialize_AcceptsJsonAndLegacyValues(string value, params string[] expected)
    {
        Assert.Equal(expected, UserRoleSerialization.Deserialize(value));
    }
}
