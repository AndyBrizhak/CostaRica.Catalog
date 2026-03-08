using CostaRica.Api.DTOs;
using CostaRica.Api.Services;

namespace CostaRica.Api.Endpoints;

public static class CitiesEndpoints
{
    public static void MapCityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/cities").WithTags("Cities");

        // GET /api/cities
        group.MapGet("/", async (ICityService service) =>
        {
            var cities = await service.GetAllAsync();
            return Results.Ok(cities);
        })
        .WithName("GetCities");

        // GET /api/cities/{id}
        group.MapGet("/{id:guid}", async (Guid id, ICityService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetCityById");

        // GET /api/cities/province/{slug}
        // Специальный метод для фильтрации городов по провинции
        group.MapGet("/province/{slug}", async (string slug, ICityService service) =>
        {
            var cities = await service.GetByProvinceAsync(slug);
            return Results.Ok(cities);
        })
        .WithName("GetCitiesByProvince");

        // POST /api/cities
        group.MapPost("/", async (CityUpsertDto dto, ICityService service) =>
        {
            var result = await service.CreateAsync(dto);

            if (result is null)
            {
                return Results.BadRequest(new
                {
                    error = "Не удалось создать город. Проверьте, существует ли ProvinceId и уникален ли Slug."
                });
            }

            return Results.Created($"/api/cities/{result.Id}", result);
        })
        .WithName("CreateCity");

        // PUT /api/cities/{id}
        group.MapPut("/{id:guid}", async (Guid id, CityUpsertDto dto, ICityService service) =>
        {
            var updated = await service.UpdateAsync(id, dto);

            if (!updated)
            {
                return Results.BadRequest(new
                {
                    error = "Обновление не удалось. Возможно, город не найден, провинция не существует или Slug уже занят."
                });
            }

            return Results.NoContent();
        })
        .WithName("UpdateCity");

        // DELETE /api/cities/{id}
        group.MapDelete("/{id:guid}", async (Guid id, ICityService service) =>
        {
            var deleted = await service.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteCity");
    }
}