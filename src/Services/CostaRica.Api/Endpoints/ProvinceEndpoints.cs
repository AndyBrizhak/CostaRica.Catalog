using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Endpoints;

public static class ProvinceEndpoints
{
    public static void MapProvinceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/provinces").WithTags("Provinces");

        // GET: Список всех провинций
        group.MapGet("/", async (DirectoryDbContext db) =>
        {
            return await db.Provinces
                .Select(p => new ProvinceResponseDto(p.Id, p.Name, p.Slug))
                .ToListAsync();
        })
        .WithName("GetProvinces");

        // GET: Получение по ID
        group.MapGet("/{id:guid}", async (Guid id, DirectoryDbContext db) =>
        {
            return await db.Provinces.FindAsync(id)
                is Province province
                    ? Results.Ok(new ProvinceResponseDto(province.Id, province.Name, province.Slug))
                    : Results.NotFound();
        })
        .WithName("GetProvinceById");

        // POST: Создание
        group.MapPost("/", async (ProvinceUpsertDto dto, DirectoryDbContext db) =>
        {
            var province = new Province
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Slug = dto.Slug
            };

            db.Provinces.Add(province);
            await db.SaveChangesAsync();

            return Results.Created($"/api/provinces/{province.Id}",
                new ProvinceResponseDto(province.Id, province.Name, province.Slug));
        })
        .WithName("CreateProvince");

        // PUT: Обновление
        group.MapPut("/{id:guid}", async (Guid id, ProvinceUpsertDto dto, DirectoryDbContext db) =>
        {
            var province = await db.Provinces.FindAsync(id);

            if (province is null) return Results.NotFound();

            province.Name = dto.Name;
            province.Slug = dto.Slug;

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("UpdateProvince");

        // DELETE: Удаление
        group.MapDelete("/{id:guid}", async (Guid id, DirectoryDbContext db) =>
        {
            var affected = await db.Provinces
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1 ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProvince");
    }
}