using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var tags = app.MapGroup("/api/tags")
            .WithTags("Tags")
            .RequireAuthorization("ManagementAccess") // Доступ: Manager+
            .WithOpenApi();

        // GET /api/tags (List)
        tags.MapGet("/", async (
            [AsParameters] TagQueryParameters parameters,
            ITagService service,
            HttpContext context,
            CancellationToken ct) =>
        {
            var (items, totalCount) = await service.GetAllAsync(parameters, ct);

            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetTags");

        // GET /api/tags/{id}
        tags.MapGet("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } tag ? Results.Ok(tag) : Results.NotFound())
            .WithName("GetTagById");

        // POST /api/tags
        tags.MapPost("/", async (TagUpsertDto dto, ITagService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);
            if (result is null)
            {
                return Results.Conflict(new { error = "Не удалось создать тег. Проверьте уникальность слага и наличие родительской группы." });
            }
            return Results.Created($"/api/tags/{result.Id}", result);
        })
        .WithName("CreateTag");

        // PUT /api/tags/{id}
        tags.MapPut("/{id:guid}", async (Guid id, TagUpsertDto dto, ITagService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);
            if (result is null)
            {
                var exists = await service.GetByIdAsync(id, ct);
                return exists is null ? Results.NotFound() : Results.Conflict(new { error = "Ошибка обновления: конфликт слага или группа не найдена." });
            }
            return Results.Ok(result);
        })
        .WithName("UpdateTag");

        // DELETE /api/tags/{id}
        tags.MapDelete("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("AdminFullAccess") // Удаление только для Admin
            .WithName("DeleteTag");
    }
}