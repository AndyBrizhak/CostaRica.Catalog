using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CostaRica.Api.Endpoints;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/media")
            .WithTags("Media")
            .RequireAuthorization("ManagementAccess");

        // 1. ПОЛУЧЕНИЕ СПИСКА (GET) с ручным парсингом параметров React Admin
        group.MapGet("/", async (HttpContext context, IMediaAssetService service, CancellationToken ct) =>
        {
            var query = context.Request.Query;
            var parameters = new MediaQueryParameters();

            // Парсинг сортировки: sort=["field","ORDER"]
            var sortJson = query["sort"].ToString();
            if (!string.IsNullOrWhiteSpace(sortJson) && sortJson.StartsWith('['))
            {
                var sortArray = JsonSerializer.Deserialize<string[]>(sortJson);
                if (sortArray?.Length == 2)
                {
                    parameters._sort = sortArray[0];
                    parameters._order = sortArray[1].ToUpper();
                }
            }

            // Парсинг пагинации: range=[0,9]
            var rangeJson = query["range"].ToString();
            if (!string.IsNullOrWhiteSpace(rangeJson) && rangeJson.StartsWith('['))
            {
                var rangeArray = JsonSerializer.Deserialize<int[]>(rangeJson);
                if (rangeArray?.Length == 2)
                {
                    parameters._start = rangeArray[0];
                    parameters._end = rangeArray[1] + 1; // React Admin использует включительный индекс
                }
            }

            // Парсинг фильтров: filter={"q":"search", "onlyOrphans":true, "id":[...]}
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson) && filterJson.StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(filterJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("q", out var qProp))
                    parameters.Q = qProp.GetString();

                if (root.TryGetProperty("onlyOrphans", out var orphanProp))
                    parameters.OnlyOrphans = orphanProp.GetBoolean();

                if (root.TryGetProperty("businessId", out var bizProp) && bizProp.TryGetGuid(out var bizId))
                    parameters.BusinessId = bizId;

                if (root.TryGetProperty("id", out var idProp))
                    parameters.Id = JsonSerializer.Deserialize<Guid[]>(idProp.GetRawText());
            }

            var (items, totalCount) = await service.GetAllAsync(parameters, ct);

            // Заголовки для пагинации на фронтенде
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetMediaList");

        // 2. ПОЛУЧЕНИЕ ПО ID (GET)
        group.MapGet("/{id:guid}", async (Guid id, IMediaAssetService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetMediaById");

        // 3. ЗАГРУЗКА ФАЙЛА (POST)
        group.MapPost("/upload", async (
            IFormFile file,
            [FromForm] string slug,
            [FromForm] string? altTextEn,
            [FromForm] string? altTextEs,
            IMediaAssetService service,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(slug))
                return Results.BadRequest(new { error = "Slug is required" });

            var dto = new MediaUploadDto(slug, altTextEn, altTextEs);

            using var stream = file.OpenReadStream();
            var result = await service.UploadAsync(stream, file.FileName, file.ContentType, dto, ct);

            return result != null
                ? Results.Created($"/media/{result.Id}", result)
                : Results.BadRequest(new { error = "Upload failed" });
        })
        .DisableAntiforgery()
        .WithName("UploadMedia");

        // 4. ОБНОВЛЕНИЕ МЕТАДАННЫХ (PUT)
        group.MapPut("/{id:guid}", async (Guid id, MediaUpdateDto dto, IMediaAssetService service, CancellationToken ct) =>
        {
            var result = await service.UpdateMetadataAsync(id, dto, ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("UpdateMediaMetadata");

        // 5. УДАЛЕНИЕ (DELETE) с обработкой конфликтов зависимостей
        group.MapDelete("/{id:guid}", async (Guid id, IMediaAssetService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);

            return result.Status switch
            {
                MediaDeleteStatus.Success => Results.NoContent(),
                MediaDeleteStatus.NotFound => Results.NotFound(),
                MediaDeleteStatus.InUse => Results.Conflict(new
                {
                    error = $"Cannot delete: this asset is used by {result.UsageCount} business pages.",
                    usageCount = result.UsageCount
                }),
                _ => Results.BadRequest()
            };
        })
        .RequireAuthorization("AdminFullAccess")
        .WithName("DeleteMedia");
    }
}