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
                        parameters._end = rangeArray[1] + 1; // react-admin uses inclusive end
                    }
                }
                catch { }
            }

            // 3. Фильтрация filter={"provinceId":"..."}
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson) && filterJson.StartsWith('{'))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("q", out var qSearch))
                        parameters.Q = qSearch.GetString();

                    if (root.TryGetProperty("Q", out var qSearchAlt))
                        parameters.Q = qSearchAlt.GetString();

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
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetCityById");

        group.MapPost("/", async (CityUpsertDto dto, ICityService service) =>
        {
            var result = await service.CreateAsync(dto);
            return result is null ? Results.BadRequest() : Results.Created($"/api/cities/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (Guid id, CityUpsertDto dto, ICityService service) =>
        {
            var result = await service.UpdateAsync(id, dto);
            return result is not null ? Results.Ok(result) : Results.BadRequest();
        });

        // ШАГ РЕФАКТОРИНГА: Защищенное удаление
        group.MapDelete("/{id:guid}", async (Guid id, ICityService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result switch
            {
                CityDeleteResult.Success => Results.NoContent(),
                CityDeleteResult.NotFound => Results.NotFound(),
                CityDeleteResult.InUse => Results.Conflict(new
                {
                    error = "Cannot delete this city because it is assigned to one or more business pages."
                }),
                _ => Results.BadRequest()
            };
        })
        .RequireAuthorization("AdminFullAccess");
    }
}