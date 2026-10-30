using System.Text.Json;

namespace Tracker.AuthService.Data;

/// <summary>
/// Reads both the current JSON-array representation and the legacy
/// comma-separated/plain-text representation stored by earlier deployments.
/// </summary>
public static class UserRoleSerialization
{
    public static List<string> Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(
                value,
                (JsonSerializerOptions?)null) ?? [];
        }
        catch (JsonException)
        {
            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }
}
