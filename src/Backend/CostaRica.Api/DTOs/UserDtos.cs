using System.Text.Json.Serialization;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Query parameters for the user list, tailored for react-admin.
/// </summary>
public class UserQueryParameters
{
    public int? _start { get; set; } = 0;
    public int? _end { get; set; } = 9;
    public string? _sort { get; set; } = "Email";
    public string? _order { get; set; } = "ASC";

    /// <summary>
    /// Global search filter (searches in Email and UserName).
    /// </summary>
    public string? q { get; set; }

    /// <summary>
    /// Filter by specific roles (used as an array for multi-selection in filters).
    /// </summary>
    public string[]? roles { get; set; }
}

/// <summary>
/// DTO for updating user information and a single role.
/// </summary>
public record UserUpdateDto(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("userName")] string UserName,
    [property: JsonPropertyName("role")] string Role // Strict "one role" logic
);