using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class DiscoveryEndpoints
{
    public static void MapDiscoveryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/discovery").WithTags("Discovery (Public)");

        // 1. Поиск карточек (с пагинацией и гео-позицией)
        group.MapGet("/search", async (
            [AsParameters] DiscoverySearchParams @params,
            IDiscoveryService discoveryService,
            CancellationToken ct) =>
        {
            var (items, totalCount) = await discoveryService.SearchAsync(@params, ct);

            // Добавляем заголовок X-Total-Count для удобства фронтенда (как в админке)
            return Results.Ok(items);
        })
        .WithName("SearchBusinesses")
        .WithOpenApi();

        // 2. Доступные провинции (Drill-down)
        group.MapGet("/provinces", async (
            [AsParameters] DiscoverySearchParams @params,
            IDiscoveryService discoveryService,
            CancellationToken ct) =>
        {
            var provinces = await discoveryService.GetAvailableProvincesAsync(@params, ct);
            return Results.Ok(provinces);
        })
        .WithName("GetDiscoveryProvinces");

        // 3. Доступные города (Drill-down)
        group.MapGet("/cities", async (
            [AsParameters] DiscoverySearchParams @params,
            IDiscoveryService discoveryService,
            CancellationToken ct) =>
        {
            var cities = await discoveryService.GetAvailableCitiesAsync(@params, ct);
            return Results.Ok(cities);
        })
        .WithName("GetDiscoveryCities");

        // 4. Доступные теги (Drill-down)
        group.MapGet("/tags", async (
            [AsParameters] DiscoverySearchParams @params,
            IDiscoveryService discoveryService,
            CancellationToken ct) =>
        {
            var tags = await discoveryService.GetAvailableTagsAsync(@params, ct);
            return Results.Ok(tags);
        })
        .WithName("GetDiscoveryTags");

        // 5. Детальная страница заведения по слагу
        group.MapGet("/page/{slug}", async (
            string slug,
            IDiscoveryService discoveryService,
            CancellationToken ct) =>
        {
            var business = await discoveryService.GetBySlugAsync(slug, ct);
            return business is not null
                ? Results.Ok(business)
                : Results.NotFound(new { Message = $"Business with slug '{slug}' not found." });
        })
        .WithName("GetBusinessBySlug");
    }
}