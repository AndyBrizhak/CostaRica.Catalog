using System.Security.Claims;
using CostaRica.Api.Data;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // Эндпоинт для входа в систему (уже есть)
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IIdentityService identityService) =>
        {
            var result = await identityService.LoginAsync(request);

            if (!result.Success)
            {
                return Results.BadRequest(new { errors = result.Errors });
            }

            return Results.Ok(new
            {
                token = result.Token,
                roles = result.Roles
            });
        })
        .WithName("Login")
        .WithOpenApi();

        // НОВЫЙ ЭНДПОИНТ: Получение данных текущего пользователя
        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            // Извлекаем данные из Claims токена
            var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value);

            return Results.Ok(new
            {
                id,
                email,
                roles
            });
        })
        .RequireAuthorization() // ТРЕБУЕТ ТОКЕН
        .WithName("GetCurrentUser")
        .WithOpenApi();
    }
}