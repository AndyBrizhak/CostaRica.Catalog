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

        // Глобальный поиск (стандарт react-admin параметр Q)
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{parameters.Q}%") ||
                EF.Functions.ILike(c.Slug, $"%{parameters.Q}%"));
        }
        else
        {
            // Точечные фильтры, если Q не задан
            if (!string.IsNullOrWhiteSpace(parameters.Name))
            {
                query = query.Where(c => EF.Functions.ILike(c.Name, $"%{parameters.Name}%"));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Slug))
            {
                query = query.Where(c => EF.Functions.ILike(c.Slug, $"%{parameters.Slug}%"));
            }
        }

        // 2. Подсчет общего количества
        var totalCount = await query.CountAsync();

        // 3. Сортировка
        string sortField = parameters._sort?.ToLower() ?? "name";
        bool isDescending = string.Equals(parameters._order, "DESC", StringComparison.OrdinalIgnoreCase);

        query = sortField switch
        {
            "name" => isDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "slug" => isDescending ? query.OrderByDescending(c => c.Slug) : query.OrderBy(c => c.Slug),
            // Сортировка по имени связанной провинции
            "provincename" => isDescending ? query.OrderByDescending(c => c.Province!.Name) : query.OrderBy(c => c.Province!.Name),
            _ => query.OrderBy(c => c.Name)
        };

        // 4. Пагинация (используем _start и _end из параметров)
        int skip = parameters._start ?? 0;
        int take = (parameters._end ?? 10) - skip;
        if (take <= 0) take = 10;

        var items = await query
            .Skip(skip)
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
        // Проверка существования провинции
        var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
        if (!provinceExists) return null;

        // Проверка уникальности слага (регистронезависимо)
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

        // Если меняется провинция, проверяем её существование
        if (city.ProvinceId != dto.ProvinceId)
        {
            var provinceExists = await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId);
            if (!provinceExists) return null;
        }

        // Если меняется слаг, проверяем его уникальность среди других записей
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

        // Возвращаем актуальные данные через GetByIdAsync (подтянет ProvinceName)
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