using System.Text.Json;
using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class BusinessPagesEndpoints
{
    public static void MapBusinessPageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/business-pages")
            .WithTags("Admin: Business Pages")
            .RequireAuthorization("ManagementAccess");

        // 1. Получение списка (GET)
        group.MapGet("/", async (HttpContext context, IBusinessPageService service, CancellationToken ct) =>
        {
            var query = context.Request.Query;
            var parameters = new BusinessPageQueryParameters();

            // Парсинг сортировки: sort=["name","ASC"]
            var sortJson = query["sort"].ToString();
            if (!string.IsNullOrWhiteSpace(sortJson) && sortJson.StartsWith('['))
            {
                var sortArray = JsonSerializer.Deserialize<string[]>(sortJson);
                if (sortArray?.Length == 2)
                {
                    parameters._sort = sortArray[0];
                    parameters._order = sortArray[1].ToUpper();
                }
            }

            // Парсинг пагинации: range=[0,9]
            var rangeJson = query["range"].ToString();
            if (!string.IsNullOrWhiteSpace(rangeJson) && rangeJson.StartsWith('['))
            {
                var rangeArray = JsonSerializer.Deserialize<int[]>(rangeJson);
                if (rangeArray?.Length == 2)
                {
                    parameters._start = rangeArray[0];
                    parameters._end = rangeArray[1] + 1; // RA присылает индекс включительно
                }
            }

            // Парсинг фильтров: filter={"q":"pizza","provinceId":"..."}
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson) && filterJson.StartsWith('{'))
            {
                var filterData = JsonSerializer.Deserialize<JsonElement>(filterJson);

                if (filterData.TryGetProperty("q", out var q))
                    parameters.q = q.GetString();

                if (filterData.TryGetProperty("provinceId", out var provId))
                    parameters.provinceId = provId.GetGuid();

                if (filterData.TryGetProperty("cityId", out var cityId))
                    parameters.cityId = cityId.GetGuid();

                if (filterData.TryGetProperty("isPublished", out var isPub))
                    parameters.isPublished = isPub.GetBoolean();

                if (filterData.TryGetProperty("languageCode", out var lang))
                    parameters.languageCode = lang.GetString();

                if (filterData.TryGetProperty("id", out var ids) && ids.ValueKind == JsonValueKind.Array)
                    parameters.id = ids.EnumerateArray().Select(x => x.GetGuid()).ToArray();
            }

            var (items, totalCount) = await service.GetAllAsync(parameters, ct);
            return Results.Ok(items).WithPaginationHeader(totalCount);
        })
        .WithName("GetBusinessPages");

        // 2. Получение по ID (GET)
        group.MapGet("/{id:guid}", async (Guid id, IBusinessPageService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetBusinessPageById");

        // 3. Создание (POST)
        group.MapPost("/", async (BusinessPageUpsertDto dto, IBusinessPageService service, CancellationToken ct) =>
        {
            var (result, conflictId, error) = await service.CreateAsync(dto, ct);

            if (conflictId.HasValue)
                return Results.Conflict(new { message = error, id = conflictId.Value });

            return result != null
                ? Results.Created($"/api/admin/business-pages/{result.Id}", result)
                : Results.BadRequest(new { message = error });
        })
        .WithName("CreateBusinessPage");

        // 4. Обновление (PUT)
        group.MapPut("/{id:guid}", async (Guid id, BusinessPageUpsertDto dto, IBusinessPageService service, CancellationToken ct) =>
        {
            var (result, conflictId, error) = await service.UpdateAsync(id, dto, ct);

            if (conflictId.HasValue)
                return Results.Conflict(new { message = error, id = conflictId.Value });

            if (result == null)
            {
                return error == "Страница не найдена." ? Results.NotFound() : Results.BadRequest(new { message = error });
            }

            return Results.Ok(result);
        })
        .WithName("UpdateBusinessPage");

        // 5. Удаление (DELETE)
        group.MapDelete("/{id:guid}", async (Guid id, IBusinessPageService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("AdminFullAccess") // Удаление только для админов
        .WithName("DeleteBusinessPage");
    }

    // Хелпер для пагинации
    public static IResult WithPaginationHeader(this IResult result, int totalCount)
    {
        return new PaginationResult(result, totalCount);
    }
}

// Внутренний класс для обработки заголовков X-Total-Count
internal class PaginationResult(IResult innerResult, int totalCount) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.Append("X-Total-Count", totalCount.ToString());
        httpContext.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");
        await innerResult.ExecuteAsync(httpContext);
    }
}