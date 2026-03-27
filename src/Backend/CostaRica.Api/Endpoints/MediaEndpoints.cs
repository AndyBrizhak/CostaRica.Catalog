using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace CostaRica.Api.Endpoints;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/media")
            .WithTags("Media")
            .RequireAuthorization("ManagementAccess");

        // 1. Получение списка (GET)
        // [AsParameters] остается только у MediaQueryParameters
        group.MapGet("/", async ([AsParameters] MediaQueryParameters @params, IMediaAssetService service, HttpContext context) =>
        {
            var (items, totalCount) = await service.GetAllAsync(@params);

            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetMediaList");

        // 2. Получение по ID (GET)
        group.MapGet("/{id:guid}", async (Guid id, IMediaAssetService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetMediaById");

        // 3. SEO-эндпоинт (GET)
        group.MapGet("/{id:guid}/{slug}", async (Guid id, string slug, IMediaAssetService service) =>
        {
            var result = await service.GetByIdAsync(id);
            if (result == null || result.Slug != slug) return Results.NotFound();
            return Results.Ok(result);
        })
        .WithName("GetMediaBySlug");

        // 4. Загрузка (POST) - Доступ: Manager+
        group.MapPost("/upload", async (
            IFormFile file,
            [FromForm] string slug,
            [FromForm] string? altTextEn,
            [FromForm] string? altTextEs,
            IMediaAssetService service) =>
        {
            if (string.IsNullOrWhiteSpace(slug))
                return Results.BadRequest("Slug обязателен");

            var dto = new MediaUploadDto(slug, altTextEn, altTextEs);

            using var stream = file.OpenReadStream();
            var result = await service.UploadAsync(stream, file.FileName, file.ContentType, dto);

            return result != null
                ? Results.Created($"/media/{result.Id}/{result.Slug}", result)
                : Results.BadRequest("Ошибка загрузки");
        })
        .DisableAntiforgery()
        .WithName("UploadMedia");

        // 5. Обновление метаданных (PUT) - Доступ: Manager+
        group.MapPut("/{id:guid}", async (Guid id, MediaUpdateDto dto, IMediaAssetService service) =>
        {
            var result = await service.UpdateMetadataAsync(id, dto);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("UpdateMediaMetadata");

        // 6. Удаление (DELETE) - Доступ: Admin+
        group.MapDelete("/{id:guid}", async (Guid id, IMediaAssetService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success
                ? Results.NoContent()
                : Results.BadRequest(new { message = result.ErrorMessage });
        })
        .RequireAuthorization("AdminFullAccess")
        .WithName("DeleteMedia");
    }
}