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
            query = query.Where(c => c.Slug.Contains(parameters.Slug.ToLower()));
        }

        // Глобальный поиск Q (имя города, слаг или имя провинции)
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{parameters.Q}%") ||
                EF.Functions.ILike(c.Slug, $"%{parameters.Q}%") ||
                (c.Province != null && EF.Functions.ILike(c.Province.Name, $"%{parameters.Q}%")));
        }

        // 2. Подсчет общего количества до применения пагинации
        var totalCount = await query.CountAsync();

        // 3. Сортировка (Золотой стандарт)
        var isAscending = string.Equals(parameters._order, "ASC", StringComparison.OrdinalIgnoreCase);

        query = parameters._sort?.ToLower() switch
        {
            "slug" => isAscending ? query.OrderBy(c => c.Slug) : query.OrderByDescending(c => c.Slug),
            "provinceid" => isAscending ? query.OrderBy(c => c.ProvinceId) : query.OrderByDescending(c => c.ProvinceId),
            "provincename" => isAscending ? query.OrderBy(c => c.Province!.Name) : query.OrderByDescending(c => c.Province!.Name),
            _ => isAscending ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name)
        };

        // 4. Пагинация (_start и _end)
        int start = parameters._start ?? 0;
        int end = parameters._end ?? 10;
        int take = Math.Max(0, end - start);

        var items = await query
            .Skip(start)
            .Take(take)
            .Select(c => new CityResponseDto(
                c.Id,
                c.Name,
                c.Slug,
                c.ProvinceId,
                c.Province != null ? c.Province.Name : null
            ))
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
            city.Province?.Name
        );
    }

    public async Task<CityResponseDto?> CreateAsync(CityUpsertDto dto)
    {
        // Проверка существования провинции
        var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
        if (!provinceExists) return null;

        // Проверка уникальности слага (регистронезависимо)
        var slugExists = await db.Cities.AnyAsync(c => c.Slug.ToLower() == dto.Slug.ToLower());
        if (slugExists) return null;

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Slug.ToLower().Trim(),
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

        // Если меняется провинция, проверяем её наличие
        if (city.ProvinceId != dto.ProvinceId)
        {
            var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
            if (!provinceExists) return false;
        }

        // Если меняется слаг, проверяем уникальность
        if (!string.Equals(city.Slug, dto.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var slugExists = await db.Cities.AnyAsync(c => c.Slug.ToLower() == dto.Slug.ToLower() && c.Id != id);
            if (slugExists) return false;
        }

        city.Name = dto.Name;
        city.Slug = dto.Slug.ToLower().Trim();
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