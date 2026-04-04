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

        // GET /api/cities
        group.MapGet("/", async (HttpContext context, ICityService service) =>
        {
            var query = context.Request.Query;
            var parameters = new CityQueryParameters();

            // 1. Парсинг Сортировки: sort=["name","ASC"]
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
                catch { /* Ошибки парсинга игнорируем, сработают значения по умолчанию */ }
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
                        // React Admin шлет индекс включительно [0,9], преобразуем для логики Skip/Take
                        parameters._end = rangeArray[1] + 1;
                    }
                }
                catch { }
            }

            // 3. Парсинг Фильтров: filter={"q":"...", "provinceId":"..."}
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;

                    // Извлекаем Q для глобального поиска
                    if (root.TryGetProperty("q", out var q)) parameters.Q = q.GetString();

                    // Извлекаем фильтр по провинции
                    if (root.TryGetProperty("provinceId", out var pId) && Guid.TryParse(pId.GetString(), out var pGuid))
                        parameters.ProvinceId = pGuid;
                }
                catch { }
            }

            var (items, totalCount) = await service.GetAllAsync(parameters);

            // Обязательные заголовки для корректной работы пагинации во фронтенде
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetCities");

        // GET /api/cities/{id}
        group.MapGet("/{id:guid}", async (Guid id, ICityService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetCityById");

        // POST /api/cities
        group.MapPost("/", async (CityUpsertDto dto, ICityService service) =>
        {
            var result = await service.CreateAsync(dto);
            return result is null
                ? Results.BadRequest(new { error = "Не удалось создать город. Проверьте уникальность Slug." })
                : Results.Created($"/api/cities/{result.Id}", result);
        })
        .WithName("CreateCity");

        // PUT /api/cities/{id}
        group.MapPut("/{id:guid}", async (Guid id, CityUpsertDto dto, ICityService service) =>
        {
            // "Золотой стандарт": возвращаем обновленный объект для синхронизации кэша фронтенда
            var result = await service.UpdateAsync(id, dto);
            return result is not null
                ? Results.Ok(result)
                : Results.BadRequest(new { error = "Обновление не удалось." });
        })
        .WithName("UpdateCity");

        // DELETE /api/cities/{id}
        group.MapDelete("/{id:guid}", async (Guid id, ICityService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("AdminFullAccess")
        .WithName("DeleteCity");
    }
}