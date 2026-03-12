using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/media").WithTags("Media");

        // SEO-эндпоинт для выдачи изображений
        group.MapGet("/{id:guid}/{slug}", async Task<IResult> (
            Guid id,
            string slug,
            HttpContext context, // Убрали [FromQuery], HttpContext берется из контейнера напрямую
            IMediaAssetService service) =>
        {
            var asset = await service.GetByIdAsync(id);

            if (asset == null) return Results.NotFound();

            if (asset.Slug != slug)
            {
                var newUrl = $"/media/{id}/{asset.Slug}{context.Request.QueryString}";
                // Исправлено: используем стандартный Redirect с флагом permanent
                return Results.Redirect(newUrl, permanent: true);
            }

            // Перенаправляем на физический файл, который обработает ImageSharp
            return Results.File(Path.Combine("/", asset.FileName), asset.ContentType);
        })
        .WithName("GetMediaFile");

        // Получение списка с фильтрацией
        group.MapGet("/", async (MediaFilterDto filter, IMediaAssetService service) =>
        {
            return Results.Ok(await service.GetFilteredAsync(filter));
        })
        .WithName("GetMediaList");

        // Загрузка нового файла
        group.MapPost("/upload", async Task<IResult> (
            HttpRequest request,
            IMediaAssetService service) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest("Ожидается multipart/form-data");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");

            if (file == null || file.Length == 0) return Results.BadRequest("Файл не выбран");

            var dto = new MediaUploadDto(
                form["slug"].ToString(),
                form["altTextEn"].ToString(),
                form["altTextEs"].ToString()
            );

            if (string.IsNullOrWhiteSpace(dto.Slug)) return Results.BadRequest("Slug обязателен");

            using var stream = file.OpenReadStream();
            var result = await service.UploadAsync(stream, file.FileName, file.ContentType, dto);

            return result != null
                ? Results.Created($"/media/{result.Id}/{result.Slug}", result)
                : Results.BadRequest("Ошибка загрузки (возможно, слаг уже занят)");
        })
        .DisableAntiforgery()
        .WithName("UploadMedia");

        // Обновление метаданных
        group.MapPut("/{id:guid}", async (Guid id, MediaUpdateDto dto, IMediaAssetService service) =>
        {
            var result = await service.UpdateMetadataAsync(id, dto);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("UpdateMediaMetadata");

        // Удаление
        group.MapDelete("/{id:guid}", async (Guid id, IMediaAssetService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success ? Results.NoContent() : Results.BadRequest(result.ErrorMessage);
        })
        .WithName("DeleteMedia");
    }
}