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
        group.MapGet("/", async (IGoogleCategoryService service) =>
        {
            var categories = await service.GetAllAsync();
            return Results.Ok(categories);
        })
        .WithName("GetGoogleCategories");

        // GET /api/google-categories/{id}
        group.MapGet("/{id:guid}", async (Guid id, IGoogleCategoryService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetGoogleCategoryById");

        // GET /api/google-categories/gcid/{gcid}
        group.MapGet("/gcid/{gcid}", async (string gcid, IGoogleCategoryService service) =>
        {
            var result = await service.GetByGcidAsync(gcid);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetGoogleCategoryByGcid");

        // POST /api/google-categories
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

        // PUT /api/google-categories/{id}
        group.MapPut("/{id:guid}", async (Guid id, GoogleCategoryUpsertDto dto, IGoogleCategoryService service) =>
        {
            var updated = await service.UpdateAsync(id, dto);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateGoogleCategory");

        // DELETE /api/google-categories/{id}
        group.MapDelete("/{id:guid}", async (Guid id, IGoogleCategoryService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteGoogleCategory");
    }
}