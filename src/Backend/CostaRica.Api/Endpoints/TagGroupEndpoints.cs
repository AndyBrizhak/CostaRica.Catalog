using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using System.Text.Json;
using System.Net;

namespace CostaRica.Api.Endpoints;

public static class TagGroupEndpoints
{
    public static void MapTagGroupEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tag-groups")
            .WithTags("Tag Groups")
            .RequireAuthorization("ManagementAccess");

        group.MapGet("/", async (HttpContext context, ITagGroupService service, CancellationToken ct) =>
        {
            var query = context.Request.Query;
            var parameters = new TagGroupQueryParameters();

            // 1. Парсинг Сортировки: sort=["NameEn","ASC"]
            var sortJson = WebUtility.UrlDecode(query["sort"].ToString());
            if (!string.IsNullOrWhiteSpace(sortJson))
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

            // 2. Парсинг Пагинации: range=[0,9]
            var rangeJson = WebUtility.UrlDecode(query["range"].ToString());
            if (!string.IsNullOrWhiteSpace(rangeJson))
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

            // 3. Глобальный поиск и точечные фильтры: filter={"q":"food"}
            var filterJson = WebUtility.UrlDecode(query["filter"].ToString());
            if (!string.IsNullOrWhiteSpace(filterJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("q", out var qProp))
                        parameters.Q = qProp.GetString();
                    if (root.TryGetProperty("nameEn", out var nameEnProp))
                        parameters.NameEn = nameEnProp.GetString();
                    if (root.TryGetProperty("nameEs", out var nameEsProp))
                        parameters.NameEs = nameEsProp.GetString();
                    if (root.TryGetProperty("slug", out var slugProp))
                        parameters.Slug = slugProp.GetString();
                }
                catch { }
            }

            var (items, totalCount) = await service.GetAllAsync(parameters, ct);

            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetTagGroups");

        group.MapGet("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } res ? Results.Ok(res) : Results.NotFound());

        group.MapPost("/", async (TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);
            return result is not null ? Results.Created($"/api/tag-groups/{result.Id}", result) : Results.Conflict();
        });

        group.MapPut("/{id:guid}", async (Guid id, TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        // ОБНОВЛЕННЫЙ ЭНДПОИНТ УДАЛЕНИЯ
        group.MapDelete("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            return result switch
            {
                TagGroupDeleteResult.Success => Results.NoContent(),
                TagGroupDeleteResult.NotFound => Results.NotFound(),
                TagGroupDeleteResult.InUse => Results.Conflict(new { error = "Cannot delete this tag group because it contains tags." }),
                _ => Results.BadRequest()
            };
        });
    }
}