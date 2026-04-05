using System.Security.Claims;
using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

/// <summary>
/// Implementation of administrative user management service.
/// Optimized for react-admin "Gold Standard" with ILike search and role-based filtering.
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DirectoryDbContext _context;

    public AdminUserService(UserManager<ApplicationUser> userManager, DirectoryDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<(IEnumerable<object> Users, int TotalCount)> GetPagedUsersAsync(UserQueryParameters parameters)
    {
        var query = _userManager.Users.AsNoTracking();

        // 1. Global Search (q) - searching in Email and UserName
        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var searchPattern = $"%{parameters.q}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email!, searchPattern) ||
                EF.Functions.ILike(u.UserName!, searchPattern));
        }

        // 2. Filter by Roles
        if (parameters.roles != null && parameters.roles.Length > 0)
        {
            query = query.Where(u => _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                .Any(x => x.ur.UserId == u.Id && parameters.roles.Contains(x.r.Name!)));
        }

        var totalCount = await query.CountAsync();

        // 3. Sorting
        if (!string.IsNullOrWhiteSpace(parameters._sort))
        {
            var field = parameters._sort.ToLower();
            var isDesc = parameters._order?.ToUpper() == "DESC";

            query = field switch
            {
                "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "username" => isDesc ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
                "roles" => isDesc
                    ? query.OrderByDescending(u => _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                        .OrderBy(n => n).FirstOrDefault())
                    : query.OrderBy(u => _context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                        .OrderBy(n => n).FirstOrDefault()),
                _ => isDesc ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id)
            };
        }

        // 4. Pagination
        int skip = parameters._start ?? 0;
        int take = (parameters._end ?? 9) - skip + 1;

        var userList = await query.Skip(skip).Take(take).ToListAsync();

        // 5. Projection with Roles (efficiently fetching roles for the page)
        var usersWithRoles = new List<object>();
        foreach (var user in userList)
        {
            var roles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.EmailConfirmed,
                roles
            });
        }

        return (usersWithRoles, totalCount);
    }

    public async Task<object?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return new { user.Id, user.UserName, user.Email, user.EmailConfirmed, roles };
    }

    public async Task<ServiceResult> UpdateUserRolesAsync(Guid id, string? email, string? userName, List<string> newRoles, ClaimsPrincipal actor)
    {
        var targetUser = await _userManager.FindByIdAsync(id.ToString());
        if (targetUser == null) return ServiceResult.Failure("User not found", 404);

        // Modification of Email/UserName is prohibited via Admin API for safety
        if (!string.IsNullOrWhiteSpace(email) && !string.Equals(targetUser.Email, email.Trim(), StringComparison.OrdinalIgnoreCase))
            return ServiceResult.Failure("Email modification is prohibited.", 400);

        if (!string.IsNullOrWhiteSpace(userName) && !string.Equals(targetUser.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase))
            return ServiceResult.Failure("Username modification is prohibited.", 400);

        var actorRoles = actor.FindAll(ClaimTypes.Role).Select(r => r.Value);
        var targetCurrentRoles = await _userManager.GetRolesAsync(targetUser);

        int actorLevel = GetMaxRoleLevel(actorRoles);
        int targetLevel = GetMaxRoleLevel(targetCurrentRoles);
        int requestedLevel = GetMaxRoleLevel(newRoles);

        if (actorLevel <= targetLevel)
            return ServiceResult.Failure("Permission denied: Target user has equal or higher rank.", 403);

        if (actorLevel <= requestedLevel)
            return ServiceResult.Failure("Permission denied: You cannot assign a role equal to or higher than your own.", 403);

        var removeResult = await _userManager.RemoveFromRolesAsync(targetUser, targetCurrentRoles);
        if (!removeResult.Succeeded) return ServiceResult.Failure("Failed to clear existing roles.");

        var addResult = await _userManager.AddToRolesAsync(targetUser, newRoles);
        if (!addResult.Succeeded) return ServiceResult.Failure("Failed to assign new roles.");

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteUserAsync(Guid id, ClaimsPrincipal actor)
    {
        var targetUser = await _userManager.FindByIdAsync(id.ToString());
        if (targetUser == null) return ServiceResult.Failure("User not found", 404);

        var actorLevel = GetMaxRoleLevel(actor.FindAll(ClaimTypes.Role).Select(r => r.Value));
        var targetLevel = GetMaxRoleLevel(await _userManager.GetRolesAsync(targetUser));

        if (actorLevel <= targetLevel)
            return ServiceResult.Failure("Insufficient permissions to delete this user.", 403);

        var result = await _userManager.DeleteAsync(targetUser);
        return result.Succeeded ? ServiceResult.Success() : ServiceResult.Failure("Delete operation failed.");
    }

    private static int GetMaxRoleLevel(IEnumerable<string> roles)
    {
        if (roles == null || !roles.Any()) return -1;
        return roles.Select(role => role switch
        {
            "SuperAdmin" => 3,
            "Admin" => 2,
            "Manager" => 1,
            "Viewer" => 0,
            _ => -1
        }).Max();
    }
}