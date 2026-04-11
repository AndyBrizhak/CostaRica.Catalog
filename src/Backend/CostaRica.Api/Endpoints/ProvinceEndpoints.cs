using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CostaRica.Api.Endpoints;

public static class ProvinceEndpoints
{
    public static void MapProvinceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/provinces")
            .WithTags("Provinces")
            .RequireAuthorization("ManagementAccess");

        // GET /api/provinces
        group.MapGet("/", async (
            HttpContext context,
            IProvinceService service,
            [FromQuery] string? filter = null,
            [FromQuery] string? range = null,
            [FromQuery] string? sort = null,
            [FromQuery] bool includeCities = false) =>
        {
            var queryParams = new ProvinceQueryParameters();

            if (!string.IsNullOrEmpty(filter))
            {
                try
                {
                    var filterData = JsonSerializer.Deserialize<Dictionary<string, object>>(filter);
                    if (filterData != null && filterData.TryGetValue("Q", out var qValue))
                    {
                        queryParams.Q = qValue?.ToString();
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(range))
            {
                try
                {
                    var rangeData = JsonSerializer.Deserialize<int[]>(range);
                    if (rangeData?.Length == 2)
                    {
                        queryParams.Start = rangeData[0];
                        queryParams.End = rangeData[1] + 1;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(sort))
            {
                try
                {
                    var sortData = JsonSerializer.Deserialize<string[]>(sort);
                    if (sortData?.Length == 2)
                    {
                        queryParams.Sort = sortData[0];
                        queryParams.Order = sortData[1];
                    }
                }
                catch { }
            }

            var (items, totalCount) = await service.GetAllAsync(queryParams, includeCities);

            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetProvinces");

        // GET /api/provinces/{id}
        group.MapGet("/{id:guid}", async (Guid id, [FromQuery] bool includeCities = false, IProvinceService service = default!) =>
        {
            var result = await service.GetByIdAsync(id, includeCities);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProvinceById");

        // POST /api/provinces
        group.MapPost("/", async (ProvinceUpsertDto dto, IProvinceService service) =>
        {
            var result = await service.CreateAsync(dto);
            if (result is null) return Results.Conflict(new { error = "Slug conflict" });
            return Results.Created($"/api/provinces/{result.Id}", result);
        })
        .WithName("CreateProvince");

        // PUT /api/provinces/{id}
        group.MapPut("/{id:guid}", async (Guid id, ProvinceUpsertDto dto, IProvinceService service) =>
        {
            var result = await service.UpdateAsync(id, dto);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("UpdateProvince");

        // DELETE /api/provinces/{id}
        group.MapDelete("/{id:guid}", async (Guid id, IProvinceService service) =>
        {
            // Вызываем защищенное удаление
            var result = await service.DeleteAsync(id);

            return result switch
            {
                ProvinceDeleteResult.Success => Results.NoContent(),
                ProvinceDeleteResult.NotFound => Results.NotFound(),
                ProvinceDeleteResult.InUse => Results.Conflict(new
                {
                    error = "Cannot delete this province because it contains cities or is assigned to business pages."
                }),
                _ => Results.StatusCode(500)
            };
        })
        .RequireAuthorization("AdminFullAccess")
        .WithName("DeleteProvince");
    }
}