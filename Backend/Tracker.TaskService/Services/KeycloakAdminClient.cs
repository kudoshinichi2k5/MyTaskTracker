using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Tracker.TaskService.Models;

namespace Tracker.TaskService.Services;

public class KeycloakAdminClient : IKeycloakAdminClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;

    // Client-credentials tokens are short-lived (Keycloak default: 60s) but
    // reused across every admin request in that window instead of fetched
    // per-call, guarded by a lock so concurrent requests don't all race to
    // refresh it at once.
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public KeycloakAdminClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _baseUrl = (config["Keycloak:BaseUrl"] ?? throw Missing("Keycloak:BaseUrl")).TrimEnd('/');
        _realm = config["Keycloak:Realm"] ?? throw Missing("Keycloak:Realm");
        _clientId = config["Keycloak:AdminClientId"] ?? throw Missing("Keycloak:AdminClientId");
        _clientSecret = config["Keycloak:AdminClientSecret"] ?? throw Missing("Keycloak:AdminClientSecret");
    }

    private static InvalidOperationException Missing(string key) =>
        new($"{key} is not configured. See appsettings.json's \"Keycloak\" section.");

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        // 30s safety margin so we never send a token that's about to expire
        // mid-request.
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
            {
                return _cachedToken; // another caller already refreshed it while we waited
            }

            var tokenUrl = $"{_baseUrl}/realms/{_realm}/protocol/openid-connect/token";
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            };

            using var response = await _http.PostAsync(tokenUrl, new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new KeycloakAdminException(
                    $"Keycloak rejected the admin service-account login ({(int)response.StatusCode}). " +
                    "Check Keycloak:AdminClientId/AdminClientSecret, and that the service account has the " +
                    "realm-management client roles 'view-users' and 'manage-users'.");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            _cachedToken = json.GetProperty("access_token").GetString()
                ?? throw new KeycloakAdminException("Keycloak's token response had no access_token.");
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32());

            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<HttpRequestMessage> AuthorizedRequestAsync(HttpMethod method, string url, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<List<AdminUserSummary>> GetUsersWithRolesAsync(CancellationToken ct = default)
    {
        var usersRequest = await AuthorizedRequestAsync(HttpMethod.Get, $"{_baseUrl}/admin/realms/{_realm}/users?max=200", ct);
        using var usersResponse = await _http.SendAsync(usersRequest, ct);
        EnsureSuccess(usersResponse, "list users");

        var users = await usersResponse.Content.ReadFromJsonAsync<List<KeycloakUserDto>>(JsonOptions, ct) ?? [];
        var summaries = new List<AdminUserSummary>();

        // N+1 by design for this scope: one role-mappings call per user.
        // Fine for the handful of users a small realm has; if this realm
        // grows into the hundreds, batch these or cache role-mappings
        // separately instead of fetching per request.
        foreach (var user in users)
        {
            var rolesRequest = await AuthorizedRequestAsync(
                HttpMethod.Get, $"{_baseUrl}/admin/realms/{_realm}/users/{user.Id}/role-mappings/realm", ct);
            using var rolesResponse = await _http.SendAsync(rolesRequest, ct);

            var roleNames = new List<string>();
            if (rolesResponse.IsSuccessStatusCode)
            {
                var roles = await rolesResponse.Content.ReadFromJsonAsync<List<KeycloakRoleDto>>(JsonOptions, ct) ?? [];
                roleNames = roles
                    .Select(r => r.Name)
                    .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith("default-roles-"))
                    .ToList();
            }

            summaries.Add(new AdminUserSummary(user.Id, user.Username, user.Email, user.Enabled, roleNames));
        }

        return summaries;
    }

    public async Task<List<string>> GetRealmRolesAsync(CancellationToken ct = default)
    {
        var request = await AuthorizedRequestAsync(HttpMethod.Get, $"{_baseUrl}/admin/realms/{_realm}/roles", ct);
        using var response = await _http.SendAsync(request, ct);
        EnsureSuccess(response, "list realm roles");

        var roles = await response.Content.ReadFromJsonAsync<List<KeycloakRoleDto>>(JsonOptions, ct) ?? [];
        return roles
            .Select(r => r.Name)
            // Keycloak-internal roles every realm has - not meaningful to
            // hand-assign from this screen.
            .Where(n => !string.IsNullOrEmpty(n)
                        && !n.StartsWith("default-roles-")
                        && n != "offline_access"
                        && n != "uma_authorization")
            .OrderBy(n => n)
            .ToList();
    }

    public async Task AssignRealmRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        var role = await GetRoleRepresentationAsync(roleName, ct);

        var request = await AuthorizedRequestAsync(
            HttpMethod.Post, $"{_baseUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm", ct);
        request.Content = JsonContent.Create(new[] { role }, options: JsonOptions);

        using var response = await _http.SendAsync(request, ct);
        EnsureSuccess(response, $"assign role '{roleName}' to user {userId}");
    }

    public async Task RemoveRealmRoleAsync(string userId, string roleName, CancellationToken ct = default)
    {
        var role = await GetRoleRepresentationAsync(roleName, ct);

        var request = await AuthorizedRequestAsync(
            HttpMethod.Delete, $"{_baseUrl}/admin/realms/{_realm}/users/{userId}/role-mappings/realm", ct);
        request.Content = JsonContent.Create(new[] { role }, options: JsonOptions);

        using var response = await _http.SendAsync(request, ct);
        EnsureSuccess(response, $"remove role '{roleName}' from user {userId}");
    }

    // Keycloak's role-mappings endpoints need the full {id, name} role
    // representation in the request body, not just the name.
    private async Task<KeycloakRoleDto> GetRoleRepresentationAsync(string roleName, CancellationToken ct)
    {
        var request = await AuthorizedRequestAsync(HttpMethod.Get, $"{_baseUrl}/admin/realms/{_realm}/roles/{roleName}", ct);
        using var response = await _http.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeycloakAdminException($"Realm role '{roleName}' does not exist.");
        }
        EnsureSuccess(response, $"look up role '{roleName}'");

        return await response.Content.ReadFromJsonAsync<KeycloakRoleDto>(JsonOptions, ct)
            ?? throw new KeycloakAdminException($"Keycloak returned an empty response for role '{roleName}'.");
    }

    private static void EnsureSuccess(HttpResponseMessage response, string action)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new KeycloakAdminException($"Keycloak Admin API call to {action} failed with status {(int)response.StatusCode}.");
        }
    }
}