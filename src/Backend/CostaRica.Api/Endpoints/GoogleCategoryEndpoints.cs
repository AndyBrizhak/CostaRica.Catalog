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
            .RequireAuthorization("ManagementAccess");

        // GET / — List with pagination and search
        group.MapGet("/", async (
            [AsParameters] GoogleCategoryQueryParameters args,
            IGoogleCategoryService service,
            HttpResponse response,
            CancellationToken ct) =>
        {
            var (items, totalCount) = await service.GetAllAsync(args, ct);

            // Essential for React Admin or custom front-end pagination
            response.Headers.Append("X-Total-Count", totalCount.ToString());
            response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetGoogleCategories");

        // GET /{id} — Get single record
        group.MapGet("/{id:guid}", async (Guid id, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetGoogleCategoryById");

        // POST / — Create new category
        group.MapPost("/", async (GoogleCategoryUpsertDto dto, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);

            if (result is null)
                return Results.Conflict(new { error = "Category with this GCID or Name already exists." });

            return Results.Created($"/api/google-categories/{result.Id}", result);
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("CreateGoogleCategory");

        // POST /bulk — Atomic Bulk Import
        group.MapPost("/bulk", async (List<GoogleCategoryImportDto> categories, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.BulkImportAsync(categories, ct);

            if (result.HasConflict)
            {
                return Results.Conflict(new
                {
                    error = result.ErrorMessage,
                    conflictType = result.ConflictType
                });
            }

            return Results.Ok(new { ImportedCount = result.ImportedCount });
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("BulkImportGoogleCategories");

        // PUT /{id} — Update with duplicate check
        group.MapPut("/{id:guid}", async (Guid id, GoogleCategoryUpsertDto dto, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var status = await service.UpdateAsync(id, dto, ct);

            return status switch
            {
                GoogleCategoryUpdateResult.Success => Results.NoContent(),
                GoogleCategoryUpdateResult.NotFound => Results.NotFound(),
                GoogleCategoryUpdateResult.Conflict => Results.Conflict(new { error = "Update failed: GCID or Name is already taken by another category." }),
                _ => Results.BadRequest()
            };
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("UpdateGoogleCategory");

        // DELETE /{id} — Delete with dependency protection
        group.MapDelete("/{id:guid}", async (Guid id, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var (status, usageCount) = await service.DeleteAsync(id, ct);

            return status switch
            {
                GoogleCategoryDeleteResult.Success => Results.NoContent(),
                GoogleCategoryDeleteResult.NotFound => Results.NotFound(),
                GoogleCategoryDeleteResult.InUse => Results.Conflict(new
                {
                    error = $"Category is used in {usageCount} business pages and cannot be deleted."
                }),
                _ => Results.BadRequest()
            };
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("DeleteGoogleCategory");
    }
}