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

        tagGroups.MapGet("/", async (ITagGroupService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetTagGroups");

        tagGroups.MapGet("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } tg ? Results.Ok(tg) : Results.NotFound())
            .WithName("GetTagGroupById");

        // Дополнительный эндпоинт для поиска по слагу (полезно для интеграции и .http файлов)
        tagGroups.MapGet("/slug/{slug}", async (string slug, ITagGroupService service, CancellationToken ct) =>
            await service.GetBySlugAsync(slug, ct) is { } tg ? Results.Ok(tg) : Results.NotFound())
            .WithName("GetTagGroupBySlug");

        tagGroups.MapPost("/", async (TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);

            if (result is null)
            {
                return Results.Conflict(new { error = $"Группа тегов со слагом '{dto.Slug}' уже существует." });
            }

            return Results.Created($"/api/tag-groups/{result.Id}", result);
        })
        .WithName("CreateTagGroup");

        tagGroups.MapPut("/{id:guid}", async (Guid id, TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);

            if (result is null)
            {
                var exists = await service.GetByIdAsync(id, ct);
                return exists is null
                    ? Results.NotFound()
                    : Results.Conflict(new { error = $"Слаг '{dto.Slug}' уже занят другой группой." });
            }

            return Results.Ok(result);
        })
        .WithName("UpdateTagGroup");

        tagGroups.MapDelete("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteTagGroup");


        // --- Теги (Tags) ---

        tags.MapGet("/", async (ITagService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetTags");

        tags.MapGet("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } t ? Results.Ok(t) : Results.NotFound())
            .WithName("GetTagById");

        tags.MapGet("/group/{groupId:guid}", async (Guid groupId, ITagService service, CancellationToken ct) =>
            Results.Ok(await service.GetByGroupIdAsync(groupId, ct)))
            .WithName("GetTagsByGroup");

        tags.MapPost("/", async (TagUpsertDto dto, ITagService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);

            if (result is null)
            {
                return Results.Conflict(new { error = $"Не удалось создать тег. Возможно, слаг '{dto.Slug}' уже занят или указанная группа не существует." });
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
                return exists is null
                    ? Results.NotFound()
                    : Results.Conflict(new { error = "Ошибка обновления: конфликт слага или родительская группа не найдена." });
            }

            return Results.Ok(result);
        })
        .WithName("UpdateTag");

        tags.MapDelete("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteTag");
    }
}