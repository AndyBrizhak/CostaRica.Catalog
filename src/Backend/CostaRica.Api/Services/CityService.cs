using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class CityService(DirectoryDbContext db) : ICityService
{
    public async Task<IEnumerable<CityResponseDto>> GetAllAsync()
    {
        return await db.Cities
            .AsNoTracking()
            .Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId))
            .ToListAsync();
    }

    public async Task<CityResponseDto?> GetByIdAsync(Guid id)
    {
        var city = await db.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return city is null ? null : new CityResponseDto(city.Id, city.Name, city.Slug, city.ProvinceId);
    }

    public async Task<IEnumerable<CityResponseDto>> GetByProvinceAsync(string provinceSlug)
    {
        return await db.Cities
            .AsNoTracking()
            .Where(c => c.Province!.Slug == provinceSlug)
            .Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId))
            .ToListAsync();
    }

    public async Task<CityResponseDto?> CreateAsync(CityUpsertDto dto)
    {
        // 1. Валидация: Проверяем, существует ли указанная провинция
        var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
        if (!provinceExists) return null;

        // 2. Валидация: Проверяем уникальность слага
        var slugExists = await db.Cities.AnyAsync(c => c.Slug == dto.Slug);
        if (slugExists) return null;

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Slug,
            ProvinceId = dto.ProvinceId
        };

        db.Cities.Add(city);
        await db.SaveChangesAsync();

        return new CityResponseDto(city.Id, city.Name, city.Slug, city.ProvinceId);
    }

    public async Task<bool> UpdateAsync(Guid id, CityUpsertDto dto)
    {
        var city = await db.Cities.FindAsync(id);
        if (city is null) return false;

        // Валидация: Проверяем, существует ли провинция (если ID изменился)
        if (city.ProvinceId != dto.ProvinceId)
        {
            var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
            if (!provinceExists) return false;
        }

        // Валидация: Проверяем уникальность слага (если он изменился)
        if (city.Slug != dto.Slug)
        {
            var slugExists = await db.Cities.AnyAsync(c => c.Slug == dto.Slug);
            if (slugExists) return false;
        }

        city.Name = dto.Name;
        city.Slug = dto.Slug;
        city.ProvinceId = dto.ProvinceId;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var city = await db.Cities.FindAsync(id);
        if (city is null) return false;

        db.Cities.Remove(city);
        await db.SaveChangesAsync();
        return true;
    }
}