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
            return Results.Created($"/api/tag-groups/{result.Id}", result);
        });

        tagGroups.MapPut("/{id:guid}", async (Guid id, TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
            await service.UpdateAsync(id, dto, ct) is { } result ? Results.Ok(result) : Results.NotFound());

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
            try
            {
                var result = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/tags/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        tags.MapPut("/{id:guid}", async (Guid id, TagUpsertDto dto, ITagService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.UpdateAsync(id, dto, ct);
                return result is not null ? Results.Ok(result) : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        tags.MapDelete("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());
    }
}