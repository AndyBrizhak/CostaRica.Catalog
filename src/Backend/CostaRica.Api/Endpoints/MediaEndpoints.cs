using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

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
            [FromServices] IMediaAssetService service) =>
        {
            var asset = await service.GetByIdAsync(id);

            if (asset == null) return Results.NotFound();

            // Если слаг не совпадает, делаем постоянный редирект на актуальный адрес
            if (asset.Slug != slug)
            {
                var newUrl = $"/media/{id}/{asset.Slug}{context.Request.QueryString}";
                return Results.Redirect(newUrl, permanent: true);
            }

            // Отдаем файл. ImageSharp.Web перехватит этот результат и применит трансформации
            return Results.File(Path.Combine("/", asset.FileName), asset.ContentType);
        })
        .WithName("GetMediaFile");

        // 2. Получение списка с фильтрацией (Исправлено: добавлен [AsParameters])
        group.MapGet("/", async ([AsParameters] MediaFilterDto filter, [FromServices] IMediaAssetService service) =>
        {
            return Results.Ok(await service.GetFilteredAsync(filter));
        })
        .WithName("GetMediaList");

        // 3. Загрузка нового файла (Multipart FormData)
        group.MapPost("/upload", async Task<IResult> (
            HttpRequest request,
            [FromServices] IMediaAssetService service) =>
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

        // 4. Обновление метаданных ассета
        group.MapPut("/{id:guid}", async (Guid id, MediaUpdateDto dto, [FromServices] IMediaAssetService service) =>
        {
            var result = await service.UpdateMetadataAsync(id, dto);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("UpdateMediaMetadata");

        // 5. Удаление ассета (с защитой от каскадного удаления)
        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IMediaAssetService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.Success ? Results.NoContent() : Results.BadRequest(result.ErrorMessage);
        })
        .WithName("DeleteMedia");
    }
}