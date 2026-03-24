using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class BusinessPagesEndpoints
{
    public static void MapBusinessPageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/business-pages")
            .WithTags("Admin: Business Pages");

        // 1. Получение списка (GET) с поддержкой пагинации React Admin
        group.MapGet("/", async (
            [AsParameters] BusinessPageQueryParameters parameters,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var (items, totalCount) = await service.GetAllAsync(parameters, ct);

            // Используем метод расширения напрямую у IResult
            return Results.Ok(items).WithPaginationHeader(totalCount);
        });

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

        // 3. Создание новой страницы (POST)
        group.MapPost("/", async (
            BusinessPageUpsertDto dto,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);
            return result is not null
                ? Results.CreatedAtRoute("GetBusinessPageById", new { id = result.Id }, result)
                : Results.BadRequest("Не удалось создать страницу. Возможно, слаг уже занят.");
        });

        // 4. Обновление страницы (PUT)
        group.MapPut("/{id:guid}", async (
            Guid id,
            BusinessPageUpsertDto dto,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        // 5. Удаление страницы (DELETE)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IBusinessPageService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        });
    }
}

// Вспомогательный класс для пагинации
public static class BusinessPaginationExtensions
{
    public static IResult WithPaginationHeader(this IResult result, int totalCount)
    {
        return new PaginationResult(result, totalCount);
    }
}

internal class PaginationResult(IResult innerResult, int totalCount) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        // Добавляем заголовок количества записей
        httpContext.Response.Headers.Append("X-Total-Count", totalCount.ToString());
        // Разрешаем фронтенду (CORS) видеть этот заголовок
        httpContext.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

        await innerResult.ExecuteAsync(httpContext);
    }
}