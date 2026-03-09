using CostaRica.Api.DTOs;
using CostaRica.Api.Services;

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
            Results.Ok(await service.GetAllAsync(ct)));

        tagGroups.MapGet("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } tg ? Results.Ok(tg) : Results.NotFound());

        tagGroups.MapPost("/", async (TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);

            if (result is null)
            {
                return Results.Conflict(new { error = $"Tag group with slug '{dto.Slug}' already exists." });
            }

            return Results.Created($"/api/tag-groups/{result.Id}", result);
        });

        tagGroups.MapPut("/{id:guid}", async (Guid id, TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);

            if (result is null)
            {
                // Проверяем, существует ли группа вообще, чтобы понять: это 404 или 409
                var exists = await service.GetByIdAsync(id, ct);
                return exists is null
                    ? Results.NotFound()
                    : Results.Conflict(new { error = $"Slug '{dto.Slug}' is already taken by another group." });
            }

            return Results.Ok(result);
        });

        tagGroups.MapDelete("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        // --- Теги (Tags) ---

        tags.MapGet("/", async (ITagService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)));

        tags.MapGet("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } t ? Results.Ok(t) : Results.NotFound());

        tags.MapGet("/group/{groupId:guid}", async (Guid groupId, ITagService service, CancellationToken ct) =>
            Results.Ok(await service.GetByGroupIdAsync(groupId, ct)));

        tags.MapPost("/", async (TagUpsertDto dto, ITagService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);

            if (result is null)
            {
                return Results.Conflict(new { error = $"Tag with slug '{dto.Slug}' already exists or group does not exist." });
            }

            return Results.Created($"/api/tags/{result.Id}", result);
        });

        tags.MapPut("/{id:guid}", async (Guid id, TagUpsertDto dto, ITagService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);

            if (result is null)
            {
                var exists = await service.GetByIdAsync(id, ct);
                return exists is null
                    ? Results.NotFound()
                    : Results.Conflict(new { error = $"Update failed. Possible slug conflict or group not found." });
            }

            return Results.Ok(result);
        });

        tags.MapDelete("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());
    }
}