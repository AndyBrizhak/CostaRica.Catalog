using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class ProvinceService(DirectoryDbContext db) : IProvinceService
{
    public async Task<(IEnumerable<ProvinceResponseDto> Items, int TotalCount)> GetAllAsync(
        ProvinceQueryParameters @params,
        bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();

        // 1. Фильтрация (Глобальный поиск через параметр Q)
        if (!string.IsNullOrWhiteSpace(@params.Q))
        {
            var lowerSearch = @params.Q.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(lowerSearch) ||
                p.Slug.ToLower().Contains(lowerSearch));
        }

        // 2. Подсчет общего количества записей ДО пагинации
        var totalCount = await query.CountAsync();

        // 3. Включение связанных данных
        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        // 4. Сортировка (Золотой стандарт react-admin)
        var isAscending = string.Equals(@params._order, "ASC", StringComparison.OrdinalIgnoreCase);

        query = @params._sort?.ToLower() switch
        {
            "slug" => isAscending ? query.OrderBy(p => p.Slug) : query.OrderByDescending(p => p.Slug),
            "name" or _ => isAscending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name)
        };

        // 5. Пагинация (Золотой стандарт: Skip/Take на основе индексов)
        var start = @params._start ?? 0;
        var end = @params._end ?? 10;
        var take = end - start;

        var items = await query
            .Skip(start)
            .Take(take > 0 ? take : 10)
            .ToListAsync();

        // Маппинг в DTO
        var dtos = items.Select(p => MapToDto(p, includeCities));

        return (dtos, totalCount);
    }

    public async Task<ProvinceResponseDto?> GetByIdAsync(Guid id, bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();
        if (includeCities) query = query.Include(p => p.Cities);

        var province = await query.FirstOrDefaultAsync(p => p.Id == id);
        return province is null ? null : MapToDto(province, includeCities);
    }

    public async Task<ProvinceResponseDto?> GetBySlugAsync(string slug, bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();
        if (includeCities) query = query.Include(p => p.Cities);

        var province = await query.FirstOrDefaultAsync(p => p.Slug == slug);
        return province is null ? null : MapToDto(province, includeCities);
    }

    public async Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto)
    {
        var exists = await db.Provinces.AnyAsync(p => p.Slug == dto.Slug);
        if (exists) return null;

        var province = new Province
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Slug
        };

        db.Provinces.Add(province);
        await db.SaveChangesAsync();

        return MapToDto(province, false);
    }

    public async Task<bool> UpdateAsync(Guid id, ProvinceUpsertDto dto)
    {
        var province = await db.Provinces.FindAsync(id);
        if (province is null) return false;

        province.Name = dto.Name;
        province.Slug = dto.Slug;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var province = await db.Provinces.FindAsync(id);
        if (province is null) return false;

        db.Provinces.Remove(province);
        await db.SaveChangesAsync();
        return true;
    }

    private static ProvinceResponseDto MapToDto(Province province, bool includeCities)
    {
        return new ProvinceResponseDto(
            province.Id,
            province.Name,
            province.Slug,
            includeCities
                ? province.Cities.Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId, province.Name))
                : null);
    }
}