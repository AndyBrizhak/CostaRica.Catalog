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
            // УРОВЕНЬ 1: Глобальный доступ (Менеджеры и выше)
            .RequireAuthorization("ManagementAccess");

        // GET /api/provinces (Золотой стандарт react-admin)
        group.MapGet("/", async (
            [AsParameters] ProvinceQueryParameters @params,
            [FromQuery] bool includeCities = false,
            IProvinceService service = default!,
            HttpResponse response = default!) =>
        {
            var (items, totalCount) = await service.GetAllAsync(@params, includeCities);

            // Добавляем обязательные заголовки для пагинации на фронтенде
            response.Headers.Append("X-Total-Count", totalCount.ToString());
            response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

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
            if (result is null)
            {
                return Results.Conflict(new { error = $"Province with slug '{dto.Slug}' already exists." });
            }
            return Results.Created($"/api/provinces/{result.Id}", result);
        })
        .WithName("CreateProvince");

        // PUT /api/provinces/{id}
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
        // УРОВЕНЬ 2: Только Админы могут удалять
        .RequireAuthorization("AdminFullAccess");
    }
}