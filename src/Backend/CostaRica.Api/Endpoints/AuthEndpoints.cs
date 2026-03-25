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

        // Эндпоинт для входа в систему
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
    }
}