using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class TagGroupService(DirectoryDbContext db) : ITagGroupService
{
    public async Task<(IEnumerable<TagGroupResponseDto> Items, int TotalCount)> GetAllAsync(TagGroupQueryParameters parameters, CancellationToken ct = default)
    {
        var query = db.TagGroups
            .AsNoTracking()
            .AsQueryable();

        // 1. Фильтрация: Глобальный поиск (Q)
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            query = query.Where(tg =>
                EF.Functions.ILike(tg.NameEn, $"%{parameters.Q}%") ||
                EF.Functions.ILike(tg.NameEs, $"%{parameters.Q}%") ||
                EF.Functions.ILike(tg.Slug, $"%{parameters.Q}%"));
        }
        else
        {
            // Точечные фильтры
            if (!string.IsNullOrWhiteSpace(parameters.NameEn))
                query = query.Where(tg => EF.Functions.ILike(tg.NameEn, $"%{parameters.NameEn}%"));

            if (!string.IsNullOrWhiteSpace(parameters.NameEs))
                query = query.Where(tg => EF.Functions.ILike(tg.NameEs, $"%{parameters.NameEs}%"));

            if (!string.IsNullOrWhiteSpace(parameters.Slug))
                query = query.Where(tg => EF.Functions.ILike(tg.Slug, $"%{parameters.Slug}%"));
        }

        var totalCount = await query.CountAsync(ct);

        // 2. Сортировка (Золотой стандарт: исправление регистра для EF.Property)
        var sortField = parameters._sort ?? "NameEn";

        // Если фронтенд прислал camelCase (например, "nameEn"), переводим в PascalCase ("NameEn")
        if (!string.IsNullOrEmpty(sortField) && char.IsLower(sortField[0]))
        {
            sortField = char.ToUpper(sortField[0]) + sortField.Substring(1);
        }

        try
        {
            query = parameters._order?.ToUpper() == "DESC"
                ? query.OrderByDescending(tg => EF.Property<object>(tg, sortField))
                : query.OrderBy(tg => EF.Property<object>(tg, sortField));
        }
        catch (Exception)
        {
            // Фолбэк на случай, если поле вообще не существует
            query = query.OrderBy(tg => tg.NameEn);
        }

        // 3. Пагинация
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 9;
        var take = end - start + 1;

        var items = await query
            .Skip(start)
            .Take(take > 0 ? take : 10)
            .ToListAsync(ct);

        return (items.Select(MapToDto), totalCount);
    }

    public async Task<TagGroupResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var group = await db.TagGroups.FindAsync([id], ct);
        return group != null ? MapToDto(group) : null;
    }

    public async Task<TagGroupResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var group = await db.TagGroups.FirstOrDefaultAsync(tg => tg.Slug == slug.ToLower(), ct);
        return group != null ? MapToDto(group) : null;
    }

    public async Task<TagGroupResponseDto?> CreateAsync(TagGroupUpsertDto dto, CancellationToken ct = default)
    {
        var slugLower = dto.Slug.ToLower().Trim();
        if (await db.TagGroups.AnyAsync(tg => tg.Slug == slugLower, ct)) return null;

        var group = new TagGroup
        {
            Id = Guid.NewGuid(),
            NameEn = dto.NameEn,
            NameEs = dto.NameEs,
            Slug = slugLower
        };

        db.TagGroups.Add(group);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(group.Id, ct);
    }

    public async Task<TagGroupResponseDto?> UpdateAsync(Guid id, TagGroupUpsertDto dto, CancellationToken ct = default)
    {
        var group = await db.TagGroups.FindAsync([id], ct);
        if (group == null) return null;

        var slugLower = dto.Slug.ToLower().Trim();
        if (group.Slug != slugLower && await db.TagGroups.AnyAsync(tg => tg.Slug == slugLower, ct))
            return null;

        group.NameEn = dto.NameEn;
        group.NameEs = dto.NameEs;
        group.Slug = slugLower;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var group = await db.TagGroups
            .Include(tg => tg.Tags)
            .FirstOrDefaultAsync(tg => tg.Id == id, ct);

        if (group == null || group.Tags.Any()) return false;

        db.TagGroups.Remove(group);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TagGroupResponseDto MapToDto(TagGroup tg) =>
        new(tg.Id, tg.NameEn, tg.NameEs, tg.Slug);
}