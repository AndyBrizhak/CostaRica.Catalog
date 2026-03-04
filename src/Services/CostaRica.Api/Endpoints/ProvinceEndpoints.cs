using CostaRica.Api.DTOs;
using CostaRica.Api.Services;

namespace CostaRica.Api.Endpoints;

public static class ProvinceEndpoints
{
    public static void MapProvinceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/provinces").WithTags("Provinces");

        group.MapGet("/", async (IProvinceService service) =>
        {
            var provinces = await service.GetAllAsync();
            return Results.Ok(provinces);
        })
        .WithName("GetProvinces");

        group.MapGet("/{id:guid}", async (Guid id, IProvinceService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProvinceById");

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