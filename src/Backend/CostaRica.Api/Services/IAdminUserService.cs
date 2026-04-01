using System.Security.Claims;
using CostaRica.Api.Data;

namespace CostaRica.Api.Services;

public interface IAdminUserService
{
    Task<(IEnumerable<object> Users, int TotalCount)> GetPagedUsersAsync(string? range, string? sort);
    Task<object?> GetUserByIdAsync(Guid id);
    Task<ServiceResult> UpdateUserRolesAsync(Guid id, string? email, string? userName, List<string> newRoles, ClaimsPrincipal actor);
    Task<ServiceResult> DeleteUserAsync(Guid id, ClaimsPrincipal actor);
}

public class ServiceResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; } = 200;

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Failure(string message, int statusCode = 400) =>
        new() { Succeeded = false, ErrorMessage = message, StatusCode = statusCode };
}