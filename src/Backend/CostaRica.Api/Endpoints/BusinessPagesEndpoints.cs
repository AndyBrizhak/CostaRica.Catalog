using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class BusinessPagesEndpoints
{
    public static void MapBusinessPageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/business-pages")
            .WithTags("Admin: Business Pages")
            .RequireAuthorization("ManagementAccess");

        // 1. Получение списка (GET)
        group.MapGet("/", async (
            [AsParameters] BusinessPageQueryParameters parameters,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var (items, totalCount) = await service.GetAllAsync(parameters, ct);
            return Results.Ok(items).WithPaginationHeader(totalCount);
        })
        .WithName("GetBusinessPages");

        // 2. Получение одной страницы по ID (GET)
        group.MapGet("/{id:guid}", async (
            Guid id,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var business = await service.GetByIdAsync(id, ct);
            return business is not null ? Results.Ok(business) : Results.NotFound();
        })
        .WithName("GetBusinessPageById");

        // 3. Создание новой страницы (POST) с обработкой конфликтов
        group.MapPost("/", async (
            BusinessPageUpsertDto dto,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var (result, conflictId, error) = await service.CreateAsync(dto, ct);

            if (conflictId.HasValue)
                return Results.Conflict(new { message = error, id = conflictId.Value });

            if (result == null)
                return Results.BadRequest(new { message = error ?? "Ошибка при создании страницы." });

            return Results.Created($"/api/admin/business-pages/{result.Id}", result);
        })
        .WithName("CreateBusinessPage");

        // 4. Обновление страницы (PUT) с обработкой конфликтов
        group.MapPut("/{id:guid}", async (
            Guid id,
            BusinessPageUpsertDto dto,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var (result, conflictId, error) = await service.UpdateAsync(id, dto, ct);

            if (conflictId.HasValue)
                return Results.Conflict(new { message = error, id = conflictId.Value });

            if (result == null)
            {
                if (error == "Страница не найдена.") return Results.NotFound();
                return Results.BadRequest(new { message = error ?? "Ошибка при обновлении страницы." });
            }

            return Results.Ok(result);
        })
        .WithName("UpdateBusinessPage");

        // 5. Удаление страницы (DELETE)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization("AdminFullAccess")
        .WithName("DeleteBusinessPage");
    }

    // Вспомогательный метод для пагинации
    public static IResult WithPaginationHeader(this IResult result, int totalCount)
    {
        return new PaginationResult(result, totalCount);
    }
}

internal class PaginationResult(IResult innerResult, int totalCount) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.Append("X-Total-Count", totalCount.ToString());
        httpContext.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");
        await innerResult.ExecuteAsync(httpContext);
    }
}