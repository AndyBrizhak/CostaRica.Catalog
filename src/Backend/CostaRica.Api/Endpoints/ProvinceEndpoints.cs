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
            .RequireAuthorization("ManagementAccess");

        // GET /api/provinces
        group.MapGet("/", async (
            [AsParameters] ProvinceQueryParameters @params,
            [FromQuery] bool includeCities = false,
            IProvinceService service = default!,
            HttpContext context = default!) =>
        {
            var (items, totalCount) = await service.GetAllAsync(@params, includeCities);

            // Добавляем обязательные заголовки для пагинации react-admin
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
            // Обновленный метод теперь возвращает созданный объект для синхронизации фронтенда
            var result = await service.UpdateAsync(id, dto);

            if (result is null)
            {
                return Results.NotFound(new { error = "Province not found or slug conflict." });
            }

            return Results.Ok(result);
        })
        .WithName("UpdateProvince");

        // DELETE /api/provinces/{id}
        group.MapDelete("/{id:guid}", async (Guid id, IProvinceService service) =>
        {
            // Проверяем существование, чтобы отличить "Не найдено" от "Нельзя удалить из-за связей"
            var existing = await service.GetByIdAsync(id);
            if (existing is null) return Results.NotFound();

            var success = await service.DeleteAsync(id);

            if (!success)
            {
                // Если запись есть, но не удалена — значит сработал FK Check (есть города)
                return Results.Conflict(new
                {
                    error = "Cannot delete province because it has associated cities. Delete cities first."
                });
            }

            return Results.NoContent();
        })
        .RequireAuthorization("AdminFullAccess")
        .WithName("DeleteProvince");
    }
}