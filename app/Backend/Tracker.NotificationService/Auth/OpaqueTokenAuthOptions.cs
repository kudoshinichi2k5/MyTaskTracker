using Microsoft.AspNetCore.Authentication;

namespace Tracker.NotificationService.Auth;

public sealed class OpaqueTokenAuthOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "Bearer";

    public string AuthServiceBaseUrl { get; set; } =
        "http://localhost:5001";

    public string VerifyPath { get; set; } =
        "verify";
}   