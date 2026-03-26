using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class GoogleCategoryEndpoints
{
    public static void MapGoogleCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/google-categories")
            .WithTags("GoogleCategories")
            // УРОВЕНЬ 1: Доступ только для персонала (Manager и выше)
            .RequireAuthorization("ManagementAccess");

        // GET / — Просмотр списка (Доступно всем в ManagementAccess)
        group.MapGet("/", async (
            [AsParameters] GoogleCategoryQueryParameters args,
            IGoogleCategoryService service,
            HttpResponse response,
            CancellationToken ct) =>
        {
            var (items, totalCount) = await service.GetAllAsync(args, ct);

            response.Headers.Append("X-Total-Count", totalCount.ToString());
            response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetGoogleCategories");

        // GET /{id} — Получение одной записи (Доступно всем в ManagementAccess)
        group.MapGet("/{id:guid}", async (Guid id, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetGoogleCategoryById");

        // GET /gcid/{gcid} (Доступно всем в ManagementAccess)
        group.MapGet("/gcid/{gcid}", async (string gcid, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.GetByGcidAsync(gcid, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetGoogleCategoryByGcid");

        // --- МЕТОДЫ ТОЛЬКО ДЛЯ SUPER ADMIN ---

        // POST / — Создание одиночной категории
        group.MapPost("/", async (GoogleCategoryUpsertDto dto, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);
            if (result is null)
                return Results.Conflict(new { error = $"Category with Gcid '{dto.Gcid}' already exists." });

            return Results.Created($"/api/google-categories/{result.Id}", result);
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("CreateGoogleCategory");

        // POST /bulk — Массовый импорт
        group.MapPost("/bulk", async (List<GoogleCategoryImportDto> categories, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var count = await service.BulkImportAsync(categories, ct);
            return Results.Ok(new { ImportedCount = count });
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("BulkImportGoogleCategories");

        // PUT /{id} — Обновление
        group.MapPut("/{id:guid}", async (Guid id, GoogleCategoryUpsertDto dto, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, dto, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("UpdateGoogleCategory");

        // DELETE /{id} — Удаление
        group.MapDelete("/{id:guid}", async (Guid id, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("DeleteGoogleCategory");
    }
}