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

        // Вход в систему
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
                id = result.UserId, // Поле для парсинга в .http файлах
                token = result.Token,
                roles = result.Roles
            });
        })
        .WithName("Login")
        .WithOpenApi();

        // Публичная регистрация
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IIdentityService identityService) =>
        {
            var result = await identityService.RegisterAsync(request, "Viewer");

            if (!result.Success)
            {
                return Results.BadRequest(new { errors = result.Errors });
            }

            return Results.Ok(new
            {
                id = result.UserId, // Поле для парсинга в .http файлах
                token = result.Token,
                roles = result.Roles
            });
        })
        .WithName("Register")
        .WithOpenApi();

        // Получение данных профиля
        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
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
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithOpenApi();
    }
}