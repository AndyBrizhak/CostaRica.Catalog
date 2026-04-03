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

            // 1. Извлекаем базовые параметры. Приводим _order к ВЕРХНЕМУ регистру.
            var parameters = new CityQueryParameters
            {
                _start = int.TryParse(query["_start"], out var s) ? s : 0,
                _end = int.TryParse(query["_end"], out var e) ? e : 10,
                _sort = !string.IsNullOrWhiteSpace(query["_sort"]) ? query["_sort"].ToString() : "Name",
                _order = !string.IsNullOrWhiteSpace(query["_order"]) ? query["_order"].ToString().ToUpper() : "ASC"
            };

            // 2. Разбираем JSON-фильтр
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;

                    // ПРОВЕРКА Q и q (Case-insensitive)
                    if (root.TryGetProperty("q", out var qLow)) parameters.Q = qLow.GetString();
                    else if (root.TryGetProperty("Q", out var qUp)) parameters.Q = qUp.GetString();

                    // Фильтр по провинции
                    if (root.TryGetProperty("provinceId", out var pId) && Guid.TryParse(pId.GetString(), out var pGuid))
                        parameters.ProvinceId = pGuid;

                    // Остальные поля
                    if (root.TryGetProperty("name", out var n)) parameters.Name = n.GetString();
                    if (root.TryGetProperty("slug", out var sl)) parameters.Slug = sl.GetString();
                }
                catch { /* Игнорируем ошибки парсинга */ }
            }

            var (items, totalCount) = await service.GetAllAsync(parameters);

            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetCities");

        // Остальные методы (GetById, Post, Put, Delete) остаются без изменений
        group.MapGet("/{id:guid}", async (Guid id, ICityService service) =>
            await service.GetByIdAsync(id) is { } city ? Results.Ok(city) : Results.NotFound());

        group.MapPost("/", async (CityUpsertDto dto, ICityService service) =>
        {
            var result = await service.CreateAsync(dto);
            return result is null ? Results.BadRequest() : Results.Created($"/api/cities/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (Guid id, CityUpsertDto dto, ICityService service) =>
            await service.UpdateAsync(id, dto) ? Results.NoContent() : Results.BadRequest());

        group.MapDelete("/{id:guid}", async (Guid id, ICityService service) =>
            await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("AdminFullAccess");
    }
}