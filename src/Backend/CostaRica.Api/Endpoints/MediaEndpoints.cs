using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace CostaRica.Api.Endpoints;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/media").WithTags("Media");

        // 1. Получение списка медиа-файлов (React Admin Ready)
        group.MapGet("/", async Task<IResult> ([AsParameters] MediaQueryParameters @params, [FromServices] IMediaAssetService service, HttpContext context) =>
        {
            var (items, totalCount) = await service.GetAllAsync(@params);

            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetMediaList");

        // 2. Получение одного ассета по ID
        group.MapGet("/{id:guid}", async Task<IResult> (Guid id, [FromServices] IMediaAssetService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetMediaById");

        // 3. SEO-эндпоинт для выдачи физических файлов
        group.MapGet("/{id:guid}/{slug}", async Task<IResult> (
            Guid id,
            string slug,
            HttpContext context,
            [FromServices] IMediaAssetService service,
            [FromServices] IConfiguration config) =>
        {
            var asset = await service.GetByIdAsync(id);

            if (asset == null) return Results.NotFound();

            if (asset.Slug != slug)
            {
                var newUrl = $"/media/{id}/{asset.Slug}{context.Request.QueryString}";
                return Results.Redirect(newUrl, permanent: true);
            }

            var storagePath = config["Storage:LocalPath"] ?? "media";
            var filePath = Path.GetFullPath(Path.Combine(storagePath, asset.FileName));

            if (!File.Exists(filePath))
            {
                return Results.NotFound("Файл физически отсутствует в хранилище");
            }

            // ИСПРАВЛЕНО: В Minimal APIs используется Results.File вместо Results.PhysicalFile
            return Results.File(filePath, asset.ContentType);
        })
        .WithName("ServeMediaFile");

        // 4. Загрузка нового изображения
        group.MapPost("/upload", async Task<IResult> (
            IFormFile file,
            [FromForm] string slug,
            [FromForm] string? altTextEn,
            [FromForm] string? altTextEs,
            [FromServices] IMediaAssetService service) =>
        {
            if (string.IsNullOrWhiteSpace(slug))
                return Results.BadRequest("Slug обязателен");

            var dto = new MediaUploadDto(slug, altTextEn, altTextEs);

            using var stream = file.OpenReadStream();
            var result = await service.UploadAsync(stream, file.FileName, file.ContentType, dto);

            return result != null
                ? Results.Created($"/media/{result.Id}/{result.Slug}", result)
                : Results.BadRequest("Ошибка загрузки (возможно, этот слаг уже используется)");
        })
        .DisableAntiforgery()
        .WithName("UploadMedia");

        // 5. Обновление метаданных ассета
        group.MapPut("/{id:guid}", async Task<IResult> (Guid id, MediaUpdateDto dto, [FromServices] IMediaAssetService service) =>
        {
            var result = await service.UpdateMetadataAsync(id, dto);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("UpdateMediaMetadata");

        // 6. Удаление ассета
        group.MapDelete("/{id:guid}", async Task<IResult> (Guid id, [FromServices] IMediaAssetService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success
                ? Results.NoContent()
                : Results.BadRequest(new { message = result.ErrorMessage });
        })
        .WithName("DeleteMedia");
    }
}