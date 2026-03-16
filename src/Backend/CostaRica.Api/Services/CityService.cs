using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class CityService(DirectoryDbContext db) : ICityService
{
    public async Task<(IEnumerable<CityResponseDto> Items, int TotalCount)> GetAllAsync(CityQueryParameters parameters)
    {
        var query = db.Cities
            .AsNoTracking()
            .Include(c => c.Province)
            .AsQueryable();

        // 1. Фильтрация
        if (parameters.ProvinceId.HasValue)
        {
            query = query.Where(c => c.ProvinceId == parameters.ProvinceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Name))
        {
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{parameters.Name}%"));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Slug))
        {
            query = query.Where(c => c.Slug.Contains(parameters.Slug));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{parameters.Q}%") ||
                                    c.Slug.Contains(parameters.Q));
        }

        // 2. Подсчет общего количества до применения пагинации
        var totalCount = await query.CountAsync();

        // 3. Сортировка
        query = parameters._sort?.ToLower() switch
        {
            "name" => parameters._order == "DESC" ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "slug" => parameters._order == "DESC" ? query.OrderByDescending(c => c.Slug) : query.OrderBy(c => c.Slug),
            "provincename" => parameters._order == "DESC" ? query.OrderByDescending(c => c.Province!.Name) : query.OrderBy(c => c.Province!.Name),
            _ => query.OrderBy(c => c.Name) // Сортировка по умолчанию
        };

        // 4. Пагинация
        int start = parameters._start ?? 0;
        int end = parameters._end ?? 10;
        int pageSize = end - start;
        if (pageSize <= 0) pageSize = 10;

        var items = await query
            .Skip(start)
            .Take(pageSize)
            .Select(c => new CityResponseDto(
                c.Id,
                c.Name,
                c.Slug,
                c.ProvinceId,
                c.Province != null ? c.Province.Name : null))
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<CityResponseDto?> GetByIdAsync(Guid id)
    {
        var city = await db.Cities
            .AsNoTracking()
            .Include(c => c.Province)
            .FirstOrDefaultAsync(c => c.Id == id);

        return city is null ? null : new CityResponseDto(
            city.Id,
            city.Name,
            city.Slug,
            city.ProvinceId,
            city.Province?.Name);
    }

    public async Task<CityResponseDto?> CreateAsync(CityUpsertDto dto)
    {
        // Проверка существования провинции
        var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
        if (!provinceExists) return null;

        // Проверка уникальности слага
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

        return await GetByIdAsync(city.Id);
    }

    public async Task<bool> UpdateAsync(Guid id, CityUpsertDto dto)
    {
        var city = await db.Cities.FindAsync(id);
        if (city is null) return false;

        if (city.ProvinceId != dto.ProvinceId)
        {
            var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
            if (!provinceExists) return false;
        }

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