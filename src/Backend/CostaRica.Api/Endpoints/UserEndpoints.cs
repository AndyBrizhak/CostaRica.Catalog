using System.Security.Claims;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Admin: Users")
            .RequireAuthorization("AdminFullAccess");

        // 1. Получить постраничный список пользователей
        group.MapGet("/", async (
            [FromQuery] string? range,
            [FromQuery] string? sort,
            IAdminUserService userService,
            HttpContext context) =>
        {
            var (users, totalCount) = await userService.GetPagedUsersAsync(range, sort);

            // Рассчитываем значения для заголовка Content-Range (например, "users 0-9/50")
            // React Admin использует это для отрисовки пагинации
            int start = 0;
            int end = totalCount > 0 ? totalCount - 1 : 0;

            if (!string.IsNullOrWhiteSpace(range))
            {
                var rangeParams = System.Text.Json.JsonSerializer.Deserialize<List<int>>(range);
                if (rangeParams is { Count: 2 })
                {
                    start = rangeParams[0];
                    // Конец диапазона — это либо то, что просил фронтенд, 
                    // либо реально доступное количество записей
                    int requestedEnd = rangeParams[1];
                    int actualCount = users.Count();
                    end = start + (actualCount > 0 ? actualCount - 1 : 0);
                }
            }

            // Устанавливаем заголовки, которые мы разрешили в Program.cs
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Content-Range", $"users {start}-{end}/{totalCount}");

            return Results.Ok(users);
        })
        .WithName("GetAllUsers");

        // 2. Получить детали и роли пользователя
        group.MapGet("/{id:guid}", async (Guid id, IAdminUserService userService) =>
        {
            var result = await userService.GetUserByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetUserById");

        // 3. Удалить пользователя
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal actor,
            IAdminUserService userService) =>
        {
            var result = await userService.DeleteUserAsync(id, actor);

            if (result.Succeeded) return Results.NoContent();

            return result.StatusCode switch
            {
                404 => Results.NotFound(),
                403 => Results.Json(new { error = result.ErrorMessage }, statusCode: 403),
                _ => Results.BadRequest(new { error = result.ErrorMessage })
            };
        })
        .WithName("DeleteUser");
    }
}