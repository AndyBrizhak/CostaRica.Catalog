using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CostaRica.Api.Endpoints;

public static class CitiesEndpoints
{
    public static void MapCityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/cities")
            .WithTags("Cities")
            .RequireAuthorization("ManagementAccess");

        group.MapGet("/", async (HttpContext context, ICityService service) =>
        {
            var query = context.Request.Query;
            var parameters = new CityQueryParameters();

            // 1. Парсинг Сортировки
            var sortJson = query["sort"].ToString();
            // Оптимизация: используем char '[' вместо string "["
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

            if (string.IsNullOrEmpty(parameters._sort) || parameters._sort == "Name")
            {
                parameters._sort = query["_sort"].ToString() ?? "Name";
                parameters._order = query["_order"].ToString()?.ToUpper() ?? "ASC";
            }

            // 2. Парсинг Пагинации (Range)
            var rangeJson = query["range"].ToString();
            // Оптимизация: используем char '[' вместо string "["
            if (!string.IsNullOrWhiteSpace(rangeJson) && rangeJson.StartsWith('['))
            {
                try
                {
                    var rangeArray = JsonSerializer.Deserialize<int[]>(rangeJson);
                    if (rangeArray?.Length == 2)
                    {
                        parameters._start = rangeArray[0];
                        parameters._end = rangeArray[1] + 1;
                    }
                }
                catch { }
            }
            else
            {
                parameters._start = int.TryParse(query["_start"], out var s) ? s : 0;
                parameters._end = int.TryParse(query["_end"], out var e) ? e : 10;
            }

            // 3. Парсинг Фильтров
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("q", out var qLow)) parameters.Q = qLow.GetString();
                    else if (root.TryGetProperty("Q", out var qUp)) parameters.Q = qUp.GetString();

                    if (root.TryGetProperty("provinceId", out var pId) && Guid.TryParse(pId.GetString(), out var pGuid))
                        parameters.ProvinceId = pGuid;
                }
                catch { }
            }

            var (items, totalCount) = await service.GetAllAsync(parameters);

            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetCities");

        group.MapGet("/{id:guid}", async (Guid id, ICityService service) =>
            await service.GetByIdAsync(id) is { } city ? Results.Ok(city) : Results.NotFound());

        group.MapPost("/", async (CityUpsertDto dto, ICityService service) =>
        {
            var result = await service.CreateAsync(dto);
            return result is null ? Results.BadRequest() : Results.Created($"/api/cities/{result.Id}", result);
        });

        // PUT /api/cities/{id}
        group.MapPut("/{id:guid}", async (Guid id, CityUpsertDto dto, ICityService service) =>
        {
            var result = await service.UpdateAsync(id, dto);

            // Исправлено: возвращаем объект (Ok), а не просто 204 (NoContent)
            if (result is null)
            {
                return Results.BadRequest(new { error = "Update failed (slug conflict or not found)" });
            }

            return Results.Ok(result);
        })
        .WithName("UpdateCity");

        group.MapDelete("/{id:guid}", async (Guid id, ICityService service) =>
            await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("AdminFullAccess");
    }
}