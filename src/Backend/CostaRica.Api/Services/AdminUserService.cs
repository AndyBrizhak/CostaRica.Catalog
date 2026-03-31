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
        var query = _userManager.Users.AsNoTracking();

        // 1. Получаем общее количество до применения пагинации
        var totalCount = await query.CountAsync();

        // 2. Обработка сортировки (например, ["email","ASC"])
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

        // 3. Обработка пагинации (например, [0,9])
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

        // 4. Финальная выборка данных
        var users = await query
            .Skip(start)
            .Take(limit)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.EmailConfirmed
            })
            .ToListAsync();

        return (users, totalCount);
    }

    public async Task<object?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.EmailConfirmed,
            Roles = roles
        };
    }

    public async Task<ServiceResult> DeleteUserAsync(Guid id, ClaimsPrincipal actor)
    {
        var targetUser = await _userManager.FindByIdAsync(id.ToString());
        if (targetUser == null) return ServiceResult.Failure("Пользователь не найден", 404);

        // Проверка иерархии ролей (бизнес-логика защиты)
        var actorRoles = actor.FindAll(ClaimTypes.Role).Select(r => r.Value);
        var targetRoles = await _userManager.GetRolesAsync(targetUser);

        var actorLevel = GetMaxRoleLevel(actorRoles);
        var targetLevel = GetMaxRoleLevel(targetRoles);

        if (actorLevel <= targetLevel)
        {
            return ServiceResult.Failure("Недостаточно прав для удаления пользователя с равным или более высоким рангом", 403);
        }

        var result = await _userManager.DeleteAsync(targetUser);
        return result.Succeeded
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private static int GetMaxRoleLevel(IEnumerable<string> roles)
    {
        if (!roles.Any()) return -1;
        return roles.Max(role => role switch
        {
            "SuperAdmin" => 3,
            "Admin" => 2,
            "Manager" => 1,
            "Viewer" => 0,
            _ => -1
        });
    }
}