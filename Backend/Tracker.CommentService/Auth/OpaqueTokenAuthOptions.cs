using Microsoft.AspNetCore.Authentication;

namespace Tracker.CommentService.Auth;

/// <summary>
/// Options for the custom opaque-token bearer scheme.
/// No JWT involved: the token is a random opaque string minted by the
/// Auth Service and validated remotely on every request via /verify.
/// </summary>
public sealed class OpaqueTokenAuthOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "OpaqueBearer";

    /// <summary>Base URL of the Auth Service, e.g. http://localhost:5001</summary>
    public string AuthServiceBaseUrl { get; set; } = string.Empty;

    /// <summary>Relative path of the verification endpoint, e.g. /verify</summary>
    public string VerifyPath { get; set; } = "/verify";
}
