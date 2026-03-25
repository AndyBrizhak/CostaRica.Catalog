using System.Security.Claims;
using CostaRica.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Admin: Users")
            .RequireAuthorization("AdminFullAccess"); // Доступ только для Admin и SuperAdmin. Viewer и Manager сюда не попадут.

        // 1. Получить список всех пользователей
        group.MapGet("/", async (UserManager<ApplicationUser> userManager) =>
        {
            var users = await userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.EmailConfirmed
                })
                .ToListAsync();

            return Results.Ok(users);
        })
        .WithName("GetAllUsers");

        // 2. Получить детали и роли пользователя
        group.MapGet("/{id:guid}", async (Guid id, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null) return Results.NotFound();

            var roles = await userManager.GetRolesAsync(user);

            return Results.Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                Roles = roles
            });
        })
        .WithName("GetUserDetails");

        // 3. Обновить роли (Иерархическая логика)
        group.MapPut("/{id:guid}/roles", async (
            Guid id,
            [FromBody] List<string> newRoles,
            ClaimsPrincipal actor,
            UserManager<ApplicationUser> userManager) =>
        {
            var targetUser = await userManager.FindByIdAsync(id.ToString());
            if (targetUser == null) return Results.NotFound();

            // Определяем уровни прав
            var actorRoles = actor.FindAll(ClaimTypes.Role).Select(r => r.Value);
            var actorMaxLevel = GetMaxRoleLevel(actorRoles);

            var targetCurrentRoles = await userManager.GetRolesAsync(targetUser);
            var targetMaxLevel = GetMaxRoleLevel(targetCurrentRoles);

            var requestedMaxLevel = GetMaxRoleLevel(newRoles);

            // ПРОВЕРКА 1: Можно менять только тех, кто строго ниже тебя по рангу
            // (Супер-админ не может менять супер-админа, Админ не может менять Админа)
            if (actorMaxLevel <= targetMaxLevel)
            {
                return Results.Json(new { error = "Недостаточно прав для изменения этого пользователя." }, statusCode: 403);
            }

            // ПРОВЕРКА 2: Нельзя присваивать роли своего уровня или выше
            // (Админ не может сделать кого-то Админом или Супер-админом)
            if (actorMaxLevel <= requestedMaxLevel)
            {
                return Results.Json(new { error = "Вы не можете назначать роли своего уровня или выше." }, statusCode: 403);
            }

            await userManager.RemoveFromRolesAsync(targetUser, targetCurrentRoles);
            var result = await userManager.AddToRolesAsync(targetUser, newRoles);

            if (!result.Succeeded)
                return Results.BadRequest(result.Errors);

            return Results.NoContent();
        })
        .WithName("UpdateUserRoles");

        // 4. Удалить пользователя (Иерархическая логика)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal actor,
            UserManager<ApplicationUser> userManager) =>
        {
            var targetUser = await userManager.FindByIdAsync(id.ToString());
            if (targetUser == null) return Results.NotFound();

            var actorMaxLevel = GetMaxRoleLevel(actor.FindAll(ClaimTypes.Role).Select(r => r.Value));
            var targetCurrentRoles = await userManager.GetRolesAsync(targetUser);
            var targetMaxLevel = GetMaxRoleLevel(targetCurrentRoles);

            // ПРОВЕРКА: Удалять можно только тех, кто строго ниже по рангу
            if (actorMaxLevel <= targetMaxLevel)
            {
                return Results.Json(new { error = "Недостаточно прав для удаления этого пользователя." }, statusCode: 403);
            }

            var result = await userManager.DeleteAsync(targetUser);

            if (!result.Succeeded)
                return Results.BadRequest(result.Errors);

            return Results.NoContent();
        })
        .WithName("DeleteUser");
    }

    // Вспомогательные методы для расчета иерархии
    private static int GetRoleLevel(string role) => role switch
    {
        "SuperAdmin" => 3,
        "Admin" => 2,
        "Manager" => 1,
        "Viewer" => 0,
        _ => -1
    };

    private static int GetMaxRoleLevel(IEnumerable<string> roles) =>
        roles.Any() ? roles.Max(GetRoleLevel) : -1;
}