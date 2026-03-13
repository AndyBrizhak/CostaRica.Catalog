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

        // 1. SEO-эндпоинт для выдачи изображений с поддержкой 301 редиректа
        group.MapGet("/{id:guid}/{slug}", async Task<IResult> (
            Guid id,
            string slug,
            HttpContext context,
            [FromServices] IMediaAssetService service,
            [FromServices] IConfiguration config) =>
        {
            var asset = await service.GetByIdAsync(id);

            if (asset == null) return Results.NotFound();

            // Проверка актуальности слага для SEO. Если не совпадает — редирект на правильный URL.
            if (asset.Slug != slug)
            {
                var newUrl = $"/media/{id}/{asset.Slug}{context.Request.QueryString}";
                return Results.Redirect(newUrl, permanent: true);
            }

            // Получаем путь к хранилищу из конфигурации (по умолчанию "media")
            var storagePath = config["Storage:LocalPath"] ?? "media";

            // Формируем полный физический путь к файлу относительно рабочей директории
            var filePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), storagePath, asset.FileName));

            if (!System.IO.File.Exists(filePath))
            {
                return Results.NotFound("Файл физически не найден в папке хранилища.");
            }

            // Возвращаем файл напрямую с корректным MIME-типом
            return Results.File(filePath, asset.ContentType);
        })
        .WithName("GetMediaFile");

        // 2. Получение списка с фильтрацией (параметры передаются в строке запроса)
        group.MapGet("/", async ([AsParameters] MediaFilterDto filter, [FromServices] IMediaAssetService service) =>
        {
            return Results.Ok(await service.GetFilteredAsync(filter));
        })
        .WithName("GetMediaList");

        // 3. Загрузка нового файла (через форму для Scalar UI)
        group.MapPost("/upload", async Task<IResult> (
            [FromForm] IFormFile file,
            [FromForm] string slug,
            [FromForm] string? altTextEn,
            [FromForm] string? altTextEs,
            [FromServices] IMediaAssetService service) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest("Файл не выбран");

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
        .WithOpenApi(operation =>
        {
            operation.Summary = "Загрузить новое изображение";
            return operation;
        })
        .WithName("UploadMedia");

        // 4. Обновление метаданных ассета
        group.MapPut("/{id:guid}", async (Guid id, MediaUpdateDto dto, [FromServices] IMediaAssetService service) =>
        {
            var result = await service.UpdateMetadataAsync(id, dto);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("UpdateMediaMetadata");

        // 5. Удаление ассета
        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IMediaAssetService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success ? Results.NoContent() : Results.BadRequest(result.ErrorMessage);
        })
        .WithName("DeleteMedia");
    }
}