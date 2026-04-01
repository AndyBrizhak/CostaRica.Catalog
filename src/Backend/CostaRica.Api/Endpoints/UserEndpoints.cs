using System.Security.Claims;
using System.Text.Json.Serialization;
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

        group.MapGet("/", async ([FromQuery] string? range, [FromQuery] string? sort, IAdminUserService userService, HttpContext context) =>
        {
            var (users, totalCount) = await userService.GetPagedUsersAsync(range, sort);
            int start = 0;
            int end = totalCount > 0 ? totalCount - 1 : 0;
            if (!string.IsNullOrWhiteSpace(range))
            {
                var rangeParams = System.Text.Json.JsonSerializer.Deserialize<List<int>>(range);
                if (rangeParams is { Count: 2 })
                {
                    start = rangeParams[0];
                    int actualCount = users.Count();
                    end = start + (actualCount > 0 ? actualCount - 1 : 0);
                }
            }
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Content-Range", $"users {start}-{end}/{totalCount}");
            return Results.Ok(users);
        });

        group.MapGet("/{id:guid}", async (Guid id, IAdminUserService userService) =>
        {
            var result = await userService.GetUserByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UserUpdateDto request, ClaimsPrincipal actor, IAdminUserService userService) =>
        {
            var result = await userService.UpdateUserRolesAsync(id, request.Email, request.UserName, request.Roles, actor);
            if (result.Succeeded) return Results.Ok(await userService.GetUserByIdAsync(id));

            return result.StatusCode switch
            {
                403 => Results.Json(new { error = result.ErrorMessage }, statusCode: 403),
                _ => Results.BadRequest(new { error = result.ErrorMessage })
            };
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal actor, IAdminUserService userService) =>
        {
            var result = await userService.DeleteUserAsync(id, actor);
            return result.Succeeded ? Results.NoContent() : Results.Json(new { error = result.ErrorMessage }, statusCode: result.StatusCode);
        });
    }
}

public record UserUpdateDto(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("userName")] string UserName,
    [property: JsonPropertyName("roles")] List<string> Roles
);