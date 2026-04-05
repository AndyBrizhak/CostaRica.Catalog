using System.Security.Claims;
using System.Text.Json;
using CostaRica.Api.DTOs;
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

        // GET / - Standardized list with filtering, sorting, and pagination
        group.MapGet("/", async (HttpContext context, IAdminUserService userService) =>
        {
            var query = context.Request.Query;
            var parameters = new UserQueryParameters();

            // 1. Parse Filters (filter={"q":"...", "roles":["..."]})
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("q", out var qProp))
                        parameters.q = qProp.GetString();

                    if (root.TryGetProperty("roles", out var rolesProp) && rolesProp.ValueKind == JsonValueKind.Array)
                    {
                        parameters.roles = rolesProp.EnumerateArray()
                            .Select(x => x.GetString())
                            .Where(x => x != null)
                            .ToArray()!;
                    }
                }
                catch { /* Ignore malformed JSON */ }
            }

            // 2. Parse Sorting (sort=["field","ORDER"])
            var sortJson = query["sort"].ToString();
            if (!string.IsNullOrWhiteSpace(sortJson) && sortJson.StartsWith('['))
            {
                try
                {
                    var sortArray = JsonSerializer.Deserialize<string[]>(sortJson);
                    if (sortArray?.Length == 2)
                    {
                        parameters._sort = sortArray[0];
                        parameters._order = sortArray[1].ToUpper();
                    }
                }
                catch { }
            }

            // 3. Parse Pagination Range (range=[0,9])
            var rangeJson = query["range"].ToString();
            if (!string.IsNullOrWhiteSpace(rangeJson) && rangeJson.StartsWith('['))
            {
                try
                {
                    var rangeArray = JsonSerializer.Deserialize<int[]>(rangeJson);
                    if (rangeArray?.Length == 2)
                    {
                        parameters._start = rangeArray[0];
                        parameters._end = rangeArray[1];
                    }
                }
                catch { }
            }

            var (users, totalCount) = await userService.GetPagedUsersAsync(parameters);

            // Standard headers for react-admin
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(users);
        })
        .WithName("GetUsers");

        // GET /{id} - Get single user details
        group.MapGet("/{id:guid}", async (Guid id, IAdminUserService userService) =>
        {
            var result = await userService.GetUserByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetUserById");

        // PUT /{id} - Update user role and info
        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UserUpdateDto request, ClaimsPrincipal actor, IAdminUserService userService) =>
        {
            // Passing request.Role as a single string to the service
            var result = await userService.UpdateUserRolesAsync(id, request.Email, request.UserName, request.Role, actor);

            if (result.Succeeded)
                return Results.Ok(await userService.GetUserByIdAsync(id));

            return result.StatusCode switch
            {
                403 => Results.Json(new { error = result.ErrorMessage }, statusCode: 403),
                404 => Results.NotFound(new { error = result.ErrorMessage }),
                _ => Results.BadRequest(new { error = result.ErrorMessage })
            };
        })
        .WithName("UpdateUser");

        // DELETE /{id} - Remove user
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal actor, IAdminUserService userService) =>
        {
            var result = await userService.DeleteUserAsync(id, actor);

            if (result.Succeeded)
                return Results.NoContent();

            return Results.Json(new { error = result.ErrorMessage }, statusCode: result.StatusCode);
        })
        .WithName("DeleteUser");
    }
}