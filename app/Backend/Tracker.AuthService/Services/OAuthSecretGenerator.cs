using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Tracker.AuthService.Services;

public static class OAuthSecretGenerator
{
    public static string Create() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}
