using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class GoogleCategoryEndpoints
{
    public static void MapGoogleCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/google-categories").WithTags("GoogleCategories");

        // GET /api/google-categories
        // Теперь тоже возвращает X-Total-Count
        group.MapGet("/", async (IGoogleCategoryService service, HttpResponse response) =>
        {
            var categories = (await service.GetAllAsync()).ToList();

            response.Headers.Append("X-Total-Count", categories.Count.ToString());
            response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(categories);
        })
        .WithName("GetGoogleCategories");

        // GET /api/google-categories/search
        group.MapGet("/search", async (
            [FromQuery] string? searchTerm,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            IGoogleCategoryService service = default!,
            HttpResponse response = default!) =>
        {
            var (items, totalCount) = await service.SearchAsync(searchTerm, page, pageSize);

            response.Headers.Append("X-Total-Count", totalCount.ToString());
            response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("SearchGoogleCategories");

        // Остальные методы CRUD (без изменений)
        group.MapGet("/{id:guid}", async (Guid id, IGoogleCategoryService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetGoogleCategoryById");

        group.MapGet("/gcid/{gcid}", async (string gcid, IGoogleCategoryService service) =>
        {
            var result = await service.GetByGcidAsync(gcid);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetGoogleCategoryByGcid");

        group.MapPost("/", async (GoogleCategoryUpsertDto dto, IGoogleCategoryService service) =>
        {
            var result = await service.CreateAsync(dto);
            if (result is null)
            {
                return Results.Conflict(new { error = $"Category with Gcid '{dto.Gcid}' already exists." });
            }
            return Results.Created($"/api/google-categories/{result.Id}", result);
        })
        .WithName("CreateGoogleCategory");

        group.MapPut("/{id:guid}", async (Guid id, GoogleCategoryUpsertDto dto, IGoogleCategoryService service) =>
        {
            var updated = await service.UpdateAsync(id, dto);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateGoogleCategory");

        group.MapDelete("/{id:guid}", async (Guid id, IGoogleCategoryService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteGoogleCategory");
    }
}