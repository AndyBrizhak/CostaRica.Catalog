using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class ProvinceEndpoints
{
    public static void MapProvinceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/provinces").WithTags("Provinces");

        // GET /api/provinces?includeCities=false
        // Добавили "= false", чтобы параметр стал необязательным
        group.MapGet("/", async (IProvinceService service, [FromQuery] bool includeCities = false) =>
        {
            var provinces = await service.GetAllAsync(includeCities);
            return Results.Ok(provinces);
        })
        .WithName("GetProvinces");

        // GET /api/provinces/{id}?includeCities=false
        group.MapGet("/{id:guid}", async (Guid id, IProvinceService service, [FromQuery] bool includeCities = false) =>
        {
            var result = await service.GetByIdAsync(id, includeCities);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProvinceById");

        // GET /api/provinces/slug/{slug}?includeCities=false
        group.MapGet("/slug/{slug}", async (string slug, IProvinceService service, [FromQuery] bool includeCities = false) =>
        {
            var result = await service.GetBySlugAsync(slug, includeCities);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProvinceBySlug");

        group.MapPost("/", async (ProvinceUpsertDto dto, IProvinceService service) =>
        {
            var result = await service.CreateAsync(dto);
            if (result is null)
            {
                return Results.Conflict(new { error = $"Province with slug '{dto.Slug}' already exists." });
            }
            return Results.Created($"/api/provinces/{result.Id}", result);
        })
        .WithName("CreateProvince");

        group.MapPut("/{id:guid}", async (Guid id, ProvinceUpsertDto dto, IProvinceService service) =>
        {
            var updated = await service.UpdateAsync(id, dto);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateProvince");

        group.MapDelete("/{id:guid}", async (Guid id, IProvinceService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProvince");
    }
}