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

        // 1. Поиск (Q) — используем ILike для регистронезависимости
        if (!string.IsNullOrWhiteSpace(@params.Q))
        {
            var searchPattern = $"%{@params.Q}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, searchPattern) ||
                EF.Functions.ILike(p.Slug, searchPattern));
        }

        // 2. Общий счетчик (до пагинации)
        var totalCount = await query.CountAsync();

        // 3. Загрузка связей
        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        // 4. Сортировка (PascalCase свойства)
        var isAscending = string.Equals(@params.Order, "ASC", StringComparison.OrdinalIgnoreCase);

        query = @params.Sort?.ToLower() switch
        {
            "slug" => isAscending ? query.OrderBy(p => p.Slug) : query.OrderByDescending(p => p.Slug),
            _ => isAscending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name)
        };

        // 5. Пагинация
        if (@params.Start.HasValue && @params.End.HasValue)
        {
            var skip = @params.Start.Value;
            var take = @params.End.Value - @params.Start.Value;
            query = query.Skip(skip).Take(take);
        }

        var items = await query.ToListAsync();

        return (items.Select(p => MapToDto(p, includeCities)), totalCount);
    }

    public async Task<ProvinceResponseDto?> GetByIdAsync(Guid id, bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking();

        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        var province = await query.FirstOrDefaultAsync(p => p.Id == id);
        return province is null ? null : MapToDto(province, includeCities);
    }

    public async Task<ProvinceResponseDto?> GetBySlugAsync(string slug, bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking();

        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        var province = await query.FirstOrDefaultAsync(p => p.Slug == slug);
        return province is null ? null : MapToDto(province, includeCities);
    }

    public async Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto)
    {
        var slugExists = await db.Provinces.AnyAsync(p => p.Slug == dto.Slug);
        if (slugExists) return null;

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

    public async Task<ProvinceResponseDto?> UpdateAsync(Guid id, ProvinceUpsertDto dto)
    {
        var province = await db.Provinces.FindAsync(id);
        if (province is null) return null;

        // Проверка уникальности слага при изменении
        if (province.Slug != dto.Slug)
        {
            var slugExists = await db.Provinces.AnyAsync(p => p.Slug == dto.Slug);
            if (slugExists) return null;
        }

        province.Name = dto.Name;
        province.Slug = dto.Slug;

        await db.SaveChangesAsync();
        return MapToDto(province, false);
    }

    // --- НОВАЯ ЛОГИКА УДАЛЕНИЯ ---
    public async Task<ProvinceDeleteResult> DeleteAsync(Guid id)
    {
        // 1. Проверяем существование без загрузки связанных коллекций (Include)
        var province = await db.Provinces.FindAsync(id);
        if (province is null)
        {
            return ProvinceDeleteResult.NotFound;
        }

        // 2. Иерархическая проверка: есть ли привязанные города
        var hasCities = await db.Cities.AnyAsync(c => c.ProvinceId == id);

        // 3. Бизнес-проверка: есть ли привязанные бизнес-страницы
        var hasBusinessPages = await db.BusinessPages.AnyAsync(bp => bp.ProvinceId == id);

        // 4. Если есть зависимости, блокируем удаление
        if (hasCities || hasBusinessPages)
        {
            return ProvinceDeleteResult.InUse;
        }

        // 5. Зависимостей нет, безопасно удаляем
        db.Provinces.Remove(province);
        await db.SaveChangesAsync();

        return ProvinceDeleteResult.Success;
    }

    private static ProvinceResponseDto MapToDto(Province province, bool includeCities)
    {
        return new ProvinceResponseDto(
            province.Id,
            province.Name,
            province.Slug,
            includeCities && province.Cities != null
                ? province.Cities.Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId))
                : null
        );
    }
}