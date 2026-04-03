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
            // Исправлено: EF Core не всегда понимает StringComparison в Contains.
            // Используем проверенный ToLower() для трансляции в SQL.
            var searchSlug = parameters.Slug.ToLower();
            query = query.Where(c => c.Slug.ToLower().Contains(searchSlug));
        }

        // Глобальный поиск Q
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{parameters.Q}%") ||
                EF.Functions.ILike(c.Slug, $"%{parameters.Q}%") ||
                (c.Province != null && EF.Functions.ILike(c.Province.Name, $"%{parameters.Q}%")));
        }

        var totalCount = await query.CountAsync();

        // 2. Сортировка
        string sortField = parameters._sort?.ToLower() ?? "name";
        bool isDescending = string.Equals(parameters._order, "DESC", StringComparison.OrdinalIgnoreCase);

        query = sortField switch
        {
            "name" => isDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "slug" => isDescending ? query.OrderByDescending(c => c.Slug) : query.OrderBy(c => c.Slug),
            "provincename" => isDescending ? query.OrderByDescending(c => c.Province!.Name) : query.OrderBy(c => c.Province!.Name),
            _ => query.OrderBy(c => c.Name)
        };

        // 3. Пагинация
        var items = await query
            .Skip(parameters._start ?? 0)
            .Take((parameters._end ?? 10) - (parameters._start ?? 0))
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

        if (city is null) return null;

        return new CityResponseDto(
            city.Id,
            city.Name,
            city.Slug,
            city.ProvinceId,
            city.Province?.Name
        );
    }

    public async Task<CityResponseDto?> CreateAsync(CityUpsertDto dto)
    {
        var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
        if (!provinceExists) return null;

        // Исправлено: используем ToLower() вместо StringComparison для корректной трансляции в SQL
        var slugLower = dto.Slug.ToLower();
        var slugExists = await db.Cities.AnyAsync(c => c.Slug.ToLower() == slugLower);
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

    public async Task<CityResponseDto?> UpdateAsync(Guid id, CityUpsertDto dto)
    {
        var city = await db.Cities.FindAsync(id);
        if (city is null) return null;

        if (city.ProvinceId != dto.ProvinceId)
        {
            var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
            if (!provinceExists) return null;
        }

        // Исправлено: Убираем StringComparison из AnyAsync, так как это ломает трансляцию в SQL.
        // Сравниваем через приведение к нижнему регистру.
        var newSlugLower = dto.Slug.ToLower().Trim();
        if (city.Slug != newSlugLower)
        {
            var slugExists = await db.Cities.AnyAsync(c => c.Slug.ToLower() == newSlugLower && c.Id != id);
            if (slugExists) return null;
        }

        city.Name = dto.Name;
        city.Slug = newSlugLower;
        city.ProvinceId = dto.ProvinceId;

        await db.SaveChangesAsync();

        return await GetByIdAsync(id);
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