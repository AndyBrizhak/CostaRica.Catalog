using System.Security.Claims;
using System.Text.Json;
using CostaRica.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(IEnumerable<object> Users, int TotalCount)> GetPagedUsersAsync(string? range, string? sort)
    {
        // Получаем базовый запрос к пользователям
        var query = _userManager.Users.AsNoTracking();
        var totalCount = await query.CountAsync();

        // Логика сортировки (оставляем как есть)
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var sortParams = JsonSerializer.Deserialize<List<string>>(sort);
            if (sortParams is { Count: 2 })
            {
                var field = sortParams[0].ToLower();
                var order = sortParams[1].ToUpper();
                query = field switch
                {
                    "email" => order == "ASC" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                    "username" => order == "ASC" ? query.OrderBy(u => u.UserName) : query.OrderByDescending(u => u.UserName),
                    _ => query.OrderBy(u => u.Id)
                };
            }
        }

        // Логика пагинации (оставляем как есть)
        int start = 0;
        int end = 9;
        if (!string.IsNullOrWhiteSpace(range))
        {
            var rangeParams = JsonSerializer.Deserialize<List<int>>(range);
            if (rangeParams is { Count: 2 })
            {
                start = rangeParams[0];
                end = rangeParams[1];
            }
        }

        int limit = end - start + 1;

        // 1. Сначала получаем список пользователей для текущей страницы
        var userList = await query
            .Skip(start)
            .Take(limit)
            .ToListAsync();

        // 2. Для каждого пользователя в списке запрашиваем роли через UserManager
        var usersWithRoles = new List<object>();
        foreach (var user in userList)
        {
            // Используем встроенный метод Identity для получения ролей
            var roles = await _userManager.GetRolesAsync(user);

            usersWithRoles.Add(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.EmailConfirmed,
                Roles = roles
            });
        }

        return (usersWithRoles, totalCount);
    }

    public async Task<object?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return new { user.Id, user.UserName, user.Email, user.EmailConfirmed, Roles = roles };
    }

    public async Task<ServiceResult> UpdateUserRolesAsync(Guid id, string? email, string? userName, List<string> newRoles, ClaimsPrincipal actor)
    {
        var targetUser = await _userManager.FindByIdAsync(id.ToString());
        if (targetUser == null) return ServiceResult.Failure("User not found", 404);

        // 1. Проверка неизменности данных
        if (!string.IsNullOrWhiteSpace(email))
        {
            bool isEmailSame = string.Equals(targetUser.Email?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase);
            if (!isEmailSame) return ServiceResult.Failure("Update denied: Email modification is prohibited.", 400);
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            bool isNameSame = string.Equals(targetUser.UserName?.Trim(), userName.Trim(), StringComparison.OrdinalIgnoreCase);
            if (!isNameSame) return ServiceResult.Failure("Update denied: Username modification is prohibited.", 400);
        }

        // 2. Логика иерархии ролей
        var actorRoles = actor.FindAll(ClaimTypes.Role).Select(r => r.Value);
        var targetCurrentRoles = await _userManager.GetRolesAsync(targetUser);

        int actorLevel = GetMaxRoleLevel(actorRoles);
        int targetLevel = GetMaxRoleLevel(targetCurrentRoles);
        int requestedLevel = GetMaxRoleLevel(newRoles);

        if (actorLevel <= targetLevel)
            return ServiceResult.Failure("Permission denied: You cannot modify a user with an equal or higher rank.", 403);

        if (actorLevel <= requestedLevel)
            return ServiceResult.Failure("Permission denied: You cannot assign a role equal to or higher than your own.", 403);

        // 3. Обновление ролей
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
            return ServiceResult.Failure("Insufficient permissions to delete this user", 403);

        var result = await _userManager.DeleteAsync(targetUser);
        return result.Succeeded ? ServiceResult.Success() : ServiceResult.Failure("Delete failed");
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
        }).DefaultIfEmpty(-1).Max();
    }
}