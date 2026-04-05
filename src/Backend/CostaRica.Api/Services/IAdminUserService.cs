using System.Security.Claims;
using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Service for administrative user management.
/// Follows the "Gold Standard" pattern used in CityService.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Retrieves a paged and filtered list of users.
    /// </summary>
    /// <param name="parameters">Query parameters including pagination, sorting, and filters.</param>
    /// <returns>A tuple containing the list of users and the total count.</returns>
    Task<(IEnumerable<object> Users, int TotalCount)> GetPagedUsersAsync(UserQueryParameters parameters);

    /// <summary>
    /// Gets detailed information about a specific user.
    /// </summary>
    Task<object?> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Updates user roles and basic information.
    /// </summary>
    Task<ServiceResult> UpdateUserRolesAsync(Guid id, string? email, string? userName, List<string> newRoles, ClaimsPrincipal actor);

    /// <summary>
    /// Permanently deletes a user from the system.
    /// </summary>
    Task<ServiceResult> DeleteUserAsync(Guid id, ClaimsPrincipal actor);
}

/// <summary>
/// Result object for service operations to avoid using exceptions for flow control.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; } = 200;

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Failure(string message, int statusCode = 400) =>
        new() { Succeeded = false, ErrorMessage = message, StatusCode = statusCode };
}