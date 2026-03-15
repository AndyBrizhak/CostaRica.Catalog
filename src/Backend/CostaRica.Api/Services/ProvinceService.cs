using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class ProvinceService(DirectoryDbContext db) : IProvinceService
{
    public async Task<(IEnumerable<ProvinceResponseDto> Items, int TotalCount)> GetAllAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool isAscending = true,
        bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();

        // 1. Фильтрация (Поиск)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearch = searchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(lowerSearch) ||
                p.Slug.ToLower().Contains(lowerSearch));
        }

        // 2. Подсчет общего количества записей после фильтрации, но ДО пагинации
        var totalCount = await query.CountAsync();

        // 3. Включение связанных данных
        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        // 4. Сортировка
        query = sortBy?.ToLower() switch
        {
            "slug" => isAscending ? query.OrderBy(p => p.Slug) : query.OrderByDescending(p => p.Slug),
            _ => isAscending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name)
        };

        // 5. Пагинация
        var provinces = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 6. Маппинг в DTO
        var items = provinces.Select(p => new ProvinceResponseDto(
            p.Id,
            p.Name,
            p.Slug,
            includeCities
                ? p.Cities.Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId))
                : null
        ));

        return (items, totalCount);
    }

    public async Task<ProvinceResponseDto?> GetByIdAsync(Guid id, bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();

        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        var province = await query.FirstOrDefaultAsync(p => p.Id == id);

        if (province is null) return null;

        return MapToDto(province, includeCities);
    }

    public async Task<ProvinceResponseDto?> GetBySlugAsync(string slug, bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();

        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        var province = await query.FirstOrDefaultAsync(p => p.Slug == slug);

        if (province is null) return null;

        return MapToDto(province, includeCities);
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
                ? province.Cities.Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId))
                : null
        );
    }
}