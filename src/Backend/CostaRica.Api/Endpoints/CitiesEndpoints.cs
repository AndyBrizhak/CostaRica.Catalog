using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.Endpoints;

public static class CitiesEndpoints
{
    public static void MapCityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/cities")
            .WithTags("Cities")
            // УРОВЕНЬ 1: Доступ только для персонала (Manager, Admin, SuperAdmin)
            .RequireAuthorization("ManagementAccess");

        // GET /api/cities
        group.MapGet("/", async ([AsParameters] CityQueryParameters @params, ICityService service, HttpContext context) =>
        {
            var (items, totalCount) = await service.GetAllAsync(@params);

            // «Золотой стандарт»: передаем общее количество записей в заголовке
            context.Response.Headers.Append("X-Total-Count", totalCount.ToString());
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Total-Count");

            return Results.Ok(items);
        })
        .WithName("GetCities")
        .WithSummary("Получить список городов с пагинацией, сортировкой и фильтрами");

        // GET /api/cities/{id}
        group.MapGet("/{id:guid}", async (Guid id, ICityService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetCityById")
        .WithSummary("Получить данные конкретного города по ID");

        // POST /api/cities
        group.MapPost("/", async (CityUpsertDto dto, ICityService service) =>
        {
            var result = await service.CreateAsync(dto);

            if (result is null)
            {
                return Results.BadRequest(new
                {
                    error = "Не удалось создать город. Проверьте уникальность Slug и существование ProvinceId."
                });
            }

            return Results.Created($"/api/cities/{result.Id}", result);
        })
        .WithName("CreateCity")
        .WithSummary("Создать новый город");

        // PUT /api/cities/{id}
        group.MapPut("/{id:guid}", async (Guid id, CityUpsertDto dto, ICityService service) =>
        {
            var updated = await service.UpdateAsync(id, dto);

            if (!updated)
            {
                return Results.BadRequest(new
                {
                    error = "Обновление не удалось. Возможно, город не найден, Slug уже занят или ProvinceId не существует."
                });
            }

            return Results.NoContent();
        })
        .WithName("UpdateCity")
        .WithSummary("Обновить данные города");

        // DELETE /api/cities/{id}
        group.MapDelete("/{id:guid}", async (Guid id, ICityService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        // УРОВЕНЬ 2: Удаление разрешено только Админам и выше
        .RequireAuthorization("AdminFullAccess")
        .WithName("DeleteCity")
        .WithSummary("Удалить город (только для Администраторов)");
    }
}