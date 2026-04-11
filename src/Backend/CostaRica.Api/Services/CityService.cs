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

        var totalCount = await query.CountAsync();

        // 2. Сортировка
        var order = parameters._order?.ToUpper() == "DESC" ? "DESC" : "ASC";
        query = parameters._sort?.ToLower() switch
        {
            "slug" => order == "ASC" ? query.OrderBy(c => c.Slug) : query.OrderByDescending(c => c.Slug),
            "provincename" => order == "ASC" ? query.OrderBy(c => c.Province!.Name) : query.OrderByDescending(c => c.Province!.Name),
            _ => order == "ASC" ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name)
        };

        // 3. Пагинация
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 10;
        query = query.Skip(start).Take(end - start);

        var items = await query.ToListAsync();
        return (items.Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId, c.Province?.Name)), totalCount);
    }

    public async Task<CityResponseDto?> GetByIdAsync(Guid id)
    {
        var city = await db.Cities
            .AsNoTracking()
            .Include(c => c.Province)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (city is null) return null;

        return new CityResponseDto(city.Id, city.Name, city.Slug, city.ProvinceId, city.Province?.Name);
    }

    public async Task<CityResponseDto?> CreateAsync(CityUpsertDto dto)
    {
        var slugLower = dto.Slug.ToLower().Trim();
        if (await db.Cities.AnyAsync(c => c.Slug.ToLower() == slugLower)) return null;

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = slugLower,
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

    public async Task<CityDeleteResult> DeleteAsync(Guid id)
    {
        var city = await db.Cities.FindAsync(id);
        if (city is null) return CityDeleteResult.NotFound;

        // ШАГ РЕФАКТОРИНГА: Проверка наличия связанных бизнес-страниц
        var isUsed = await db.BusinessPages.AnyAsync(bp => bp.CityId == id);
        if (isUsed) return CityDeleteResult.InUse;

        db.Cities.Remove(city);
        await db.SaveChangesAsync();

        return CityDeleteResult.Success;
    }
}