using System.Security.Cryptography;
using System.Text;

namespace Tracker.AuthService.Services;

public static class TokenHashing
{
    public static string Compute(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
