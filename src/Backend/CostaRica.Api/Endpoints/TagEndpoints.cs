using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var tagGroups = app.MapGroup("/api/tag-groups")
            .WithTags("Tag Groups")
            .WithOpenApi();

        var tags = app.MapGroup("/api/tags")
            .WithTags("Tags")
            .WithOpenApi();

        // --- Группы тегов (Tag Groups) ---

        tagGroups.MapGet("/", async (
            [AsParameters] TagGroupQueryParameters parameters,
            ITagGroupService service,
            HttpContext context,
            CancellationToken ct) =>
        {
            var (items, totalCount) = await service.GetAllAsync(parameters, ct);

            // Установка заголовков для react-admin
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetTagGroups");

        tagGroups.MapGet("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } tg ? Results.Ok(tg) : Results.NotFound())
            .WithName("GetTagGroupById");

        tagGroups.MapGet("/slug/{slug}", async (string slug, ITagGroupService service, CancellationToken ct) =>
            await service.GetBySlugAsync(slug, ct) is { } tg ? Results.Ok(tg) : Results.NotFound())
            .WithName("GetTagGroupBySlug");

        tagGroups.MapPost("/", async (TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);
            return result is null
                ? Results.Conflict(new { error = $"Группа со слагом '{dto.Slug}' уже существует." })
                : Results.Created($"/api/tag-groups/{result.Id}", result);
        })
        .WithName("CreateTagGroup");

        tagGroups.MapPut("/{id:guid}", async (Guid id, TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);
            if (result is null)
            {
                var exists = await service.GetByIdAsync(id, ct);
                return exists is null ? Results.NotFound() : Results.Conflict(new { error = "Конфликт слага при обновлении." });
            }
            return Results.Ok(result);
        })
        .WithName("UpdateTagGroup");

        tagGroups.MapDelete("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteTagGroup");

        // --- Теги (Tags) ---

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

        tags.MapGet("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } tag ? Results.Ok(tag) : Results.NotFound())
            .WithName("GetTagById");

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

        tags.MapDelete("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteTag");
    }
}