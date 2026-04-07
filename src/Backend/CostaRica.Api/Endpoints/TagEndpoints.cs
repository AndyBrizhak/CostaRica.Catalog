using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
        tags.MapGet("/", async (HttpContext context, ITagService service, CancellationToken ct) =>
        {
            var query = context.Request.Query;
            var parameters = new TagQueryParameters();

            // 1. Сортировка sort=["field","ORDER"]
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

            // 2. Пагинация range=[0,9]
            var rangeJson = query["range"].ToString();
            if (!string.IsNullOrWhiteSpace(rangeJson) && rangeJson.StartsWith('['))
            {
                try
                {
                    var rangeArray = JsonSerializer.Deserialize<int[]>(rangeJson);
                    if (rangeArray?.Length == 2)
                    {
                        parameters._start = rangeArray[0];
                        // React Admin присылает индекс последнего элемента включительно (0-9 это 10 элементов)
                        parameters._end = rangeArray[1] + 1;
                    }
                }
                catch { }
            }

            // 3. Фильтрация filter={"q":"...", "tagGroupId":"..."}
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson) && filterJson.StartsWith('{'))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;

                    // Глобальный поиск (поддержка q и Q согласно ТЗ)
                    if (root.TryGetProperty("q", out var qProp))
                    {
                        parameters.Q = qProp.GetString();
                    }
                    else if (root.TryGetProperty("Q", out var QProp))
                    {
                        parameters.Q = QProp.GetString();
                    }

                    // Фильтр по группе
                    if (root.TryGetProperty("tagGroupId", out var groupProp) &&
                        Guid.TryParse(groupProp.GetString(), out var gGuid))
                    {
                        parameters.TagGroupId = gGuid;
                    }
                }
                catch { }
            }

            var (items, totalCount) = await service.GetAllAsync(parameters, ct);

            // Добавляем заголовки для корректной работы пагинации в ra-data-simple-rest
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
            .RequireAuthorization("AdminFullAccess") // Удаление только для полных админов
            .WithName("DeleteTag");
    }
}