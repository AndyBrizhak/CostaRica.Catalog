using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CostaRica.Api.Endpoints;

public static class TagGroupEndpoints
{
    public static void MapTagGroupEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tag-groups")
            .WithTags("Tag Groups")
            .RequireAuthorization("ManagementAccess");

        // GET /api/tag-groups (List)
        group.MapGet("/", async (HttpContext context, ITagGroupService service, CancellationToken ct) =>
        {
            var query = context.Request.Query;
            var parameters = new TagGroupQueryParameters();

            // 1. Парсинг Сортировки: sort=["NameEn","ASC"]
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
                catch { /* Игнорируем ошибки парсинга, оставляем default */ }
            }

            // 2. Парсинг Пагинации: range=[0,9]
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

            // 3. Парсинг Фильтров: filter={"q":"search","nameEn":"val"}
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson) && filterJson.StartsWith('{'))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;

                    // Глобальный поиск (проверяем q и Q)
                    if (root.TryGetProperty("q", out var qProp) || root.TryGetProperty("Q", out qProp))
                        parameters.Q = qProp.GetString();

                    // Точечные фильтры
                    if (root.TryGetProperty("nameEn", out var neProp)) parameters.NameEn = neProp.GetString();
                    if (root.TryGetProperty("nameEs", out var nesProp)) parameters.NameEs = nesProp.GetString();
                    if (root.TryGetProperty("slug", out var sProp)) parameters.Slug = sProp.GetString();
                }
                catch { }
            }

            var (items, totalCount) = await service.GetAllAsync(parameters, ct);

            // Обязательные заголовки для React Admin пагинации
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetTagGroups");

        // GET /api/tag-groups/{id}
        group.MapGet("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.GetByIdAsync(id, ct) is { } res ? Results.Ok(res) : Results.NotFound())
            .WithName("GetTagGroupById");

        // POST /api/tag-groups
        group.MapPost("/", async (TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);
            return result is not null
                ? Results.Created($"/api/tag-groups/{result.Id}", result)
                : Results.Conflict(new { error = "Slug already exists" });
        })
        .WithName("CreateTagGroup");

        // PUT /api/tag-groups/{id}
        group.MapPut("/{id:guid}", async (Guid id, TagGroupUpsertDto dto, ITagGroupService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("UpdateTagGroup");

        // DELETE /api/tag-groups/{id}
        group.MapDelete("/{id:guid}", async (Guid id, ITagGroupService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct)
                ? Results.NoContent()
                : Results.BadRequest(new { error = "Cannot delete group: it is either not found or contains tags." }))
            .WithName("DeleteTagGroup");
    }
}