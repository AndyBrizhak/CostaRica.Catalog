using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class ProvinceEndpoints
{
    public static void MapProvinceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/provinces")
            .WithTags("Provinces")
            // УРОВЕНЬ 1: Глобальный замок на всю группу. 
            // Только Менеджеры, Админы и Супер-админы могут попасть сюда.
            .RequireAuthorization("ManagementAccess");

        // GET /api/provinces (Доступ: ManagementAccess)
        group.MapGet("/", async (
            [FromQuery] string? searchTerm,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "Name",
            [FromQuery] bool isAscending = true,
            [FromQuery] bool includeCities = false,
            IProvinceService service = default!,
            HttpResponse response = default!) =>
        {
            var (items, totalCount) = await service.GetAllAsync(searchTerm, page, pageSize, sortBy, isAscending, includeCities);

            response.Headers.Append("X-Total-Count", totalCount.ToString());
            response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetProvinces");

        // GET /api/provinces/{id} (Доступ: ManagementAccess)
        group.MapGet("/{id:guid}", async (Guid id, IProvinceService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProvinceById");

        // GET /api/provinces/slug/{slug} (Доступ: ManagementAccess)
        group.MapGet("/slug/{slug}", async (string slug, IProvinceService service, [FromQuery] bool includeCities = false) =>
        {
            var result = await service.GetBySlugAsync(slug, includeCities);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProvinceBySlug");

        // POST /api/provinces (Доступ: ManagementAccess)
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

        // PUT /api/provinces/{id} (Доступ: ManagementAccess)
        group.MapPut("/{id:guid}", async (Guid id, ProvinceUpsertDto dto, IProvinceService service) =>
        {
            var updated = await service.UpdateAsync(id, dto);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateProvince");

        // DELETE /api/provinces/{id}
        group.MapDelete("/{id:guid}", async (Guid id, IProvinceService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        // УРОВЕНЬ 2: Дополнительное ограничение.
        // Только Админы и Супер-админы могут удалять записи.
        .RequireAuthorization("AdminFullAccess")
        .WithName("DeleteProvince");
    }
}