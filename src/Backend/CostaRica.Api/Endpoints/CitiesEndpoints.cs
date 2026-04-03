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

            // Создаем параметры и заполняем базовые поля пагинации/сортировки
            var parameters = new CityQueryParameters
            {
                _start = int.TryParse(query["_start"], out var s) ? s : 0,
                _end = int.TryParse(query["_end"], out var e) ? e : 10,
                _sort = query["_sort"].ToString() ?? "Name",
                _order = query["_order"].ToString() ?? "ASC"
            };

            // Разбираем JSON-фильтр: ?filter={"q":"...", "provinceId":"..."}
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("q", out var q))
                        parameters.Q = q.GetString();

                    if (root.TryGetProperty("provinceId", out var pId) && Guid.TryParse(pId.GetString(), out var pGuid))
                        parameters.ProvinceId = pGuid;

                    if (root.TryGetProperty("name", out var name))
                        parameters.Name = name.GetString();

                    if (root.TryGetProperty("slug", out var slug))
                        parameters.Slug = slug.GetString();
                }
                catch
                {
                    // Игнорируем ошибки парсинга, работаем с тем, что есть
                }
            }

            var (items, totalCount) = await service.GetAllAsync(parameters);

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
            if (result is null) return Results.BadRequest(new { error = "Slug exists or Province missing" });
            return Results.Created($"/api/cities/{result.Id}", result);
        })
        .WithName("CreateCity");

        // PUT /api/cities/{id}
        group.MapPut("/{id:guid}", async (Guid id, CityUpsertDto dto, ICityService service) =>
        {
            var updated = await service.UpdateAsync(id, dto);
            if (!updated) return Results.BadRequest(new { error = "Update failed (slug conflict or not found)" });
            return Results.NoContent();
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