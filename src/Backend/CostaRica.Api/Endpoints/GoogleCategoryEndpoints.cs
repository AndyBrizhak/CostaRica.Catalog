using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CostaRica.Api.Endpoints;

public static class GoogleCategoryEndpoints
{
    public static void MapGoogleCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/google-categories")
            .WithTags("GoogleCategories")
            .RequireAuthorization("ManagementAccess");

        // GET / — Список с умным парсингом параметров React Admin
        group.MapGet("/", async (HttpContext context, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var query = context.Request.Query;
            var parameters = new GoogleCategoryQueryParameters();

            // 1. Парсинг сортировки: sort=["field","ORDER"]
            var sortJson = query["sort"].ToString();
            if (!string.IsNullOrWhiteSpace(sortJson) && sortJson.StartsWith('['))
            {
                try
                {
                    var sortArray = JsonSerializer.Deserialize<string[]>(sortJson);
                    if (sortArray?.Length == 2)
                    {
                        parameters._sort = sortArray[0];
                        parameters._order = sortArray[1].ToUpper();
                    }
                }
                catch { /* Используем значения по умолчанию */ }
            }

            // 2. Парсинг пагинации: range=[0,9]
            var rangeJson = query["range"].ToString();
            if (!string.IsNullOrWhiteSpace(rangeJson) && rangeJson.StartsWith('['))
            {
                try
                {
                    var rangeArray = JsonSerializer.Deserialize<int[]>(rangeJson);
                    if (rangeArray?.Length == 2)
                    {
                        parameters._start = rangeArray[0];
                        parameters._end = rangeArray[1] + 1; // React Admin шлет индекс последнего элемента, SQL требует количество
                    }
                }
                catch { }
            }

            // 3. Парсинг фильтров: filter={"q":"...", "id":["..."]}
            var filterJson = query["filter"].ToString();
            if (!string.IsNullOrWhiteSpace(filterJson) && filterJson.StartsWith('{'))
            {
                try
                {
                    using var doc = JsonDocument.Parse(filterJson);

                    // Поиск по строке (q)
                    if (doc.RootElement.TryGetProperty("q", out var qProp))
                    {
                        parameters.Q = qProp.GetString();
                    }

                    // Список ID (для запросов GET_MANY)
                    if (doc.RootElement.TryGetProperty("id", out var idProp))
                    {
                        parameters.id = JsonSerializer.Deserialize<Guid[]>(idProp.GetRawText());
                    }
                }
                catch { }
            }

            var (items, totalCount) = await service.GetAllAsync(parameters, ct);

            // Пробрасываем заголовки для пагинации во фронтенд
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetGoogleCategories");

        // GET /{id} — Получение одной записи
        group.MapGet("/{id:guid}", async (Guid id, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetGoogleCategoryById");

        // POST / — Создание (только SuperAdmin)
        group.MapPost("/", async (GoogleCategoryUpsertDto dto, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/google-categories/{result.Id}", result);
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("CreateGoogleCategory");

        // POST /bulk-import — Массовый импорт
        group.MapPost("/bulk-import", async (List<GoogleCategoryImportDto> dtos, IGoogleCategoryService service, CancellationToken ct) =>
        {
            var result = await service.BulkImportAsync(dtos, ct);
            return result.HasConflict ? Results.Conflict(result) : Results.Ok(result);
        })
        .RequireAuthorization("SuperAdminOnly")
        .WithName("BulkImportGoogleCategories");

        // DELETE /{id} — Удаление с защитой от зависимостей
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
