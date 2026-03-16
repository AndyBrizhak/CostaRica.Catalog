using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

/// <summary>
/// Реализация сервиса управления группами тегов (стандарт react-admin).
/// </summary>
public class TagGroupService(DirectoryDbContext db) : ITagGroupService
{
    public async Task<(IEnumerable<TagGroupResponseDto> Items, int TotalCount)> GetAllAsync(TagGroupQueryParameters parameters, CancellationToken ct = default)
    {
        var query = db.TagGroups
            .AsNoTracking()
            .AsQueryable();

        // 1. Фильтрация по конкретным полям (используем ToLower().Contains() для совместимости с InMemory)
        if (!string.IsNullOrWhiteSpace(parameters.NameEn))
        {
            var filter = parameters.NameEn.ToLower();
            query = query.Where(tg => tg.NameEn.ToLower().Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(parameters.NameEs))
        {
            var filter = parameters.NameEs.ToLower();
            query = query.Where(tg => tg.NameEs.ToLower().Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Slug))
        {
            var filter = parameters.Slug.ToLower();
            query = query.Where(tg => tg.Slug.Contains(filter));
        }

        // 2. Глобальный поиск (Q)
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            var filter = parameters.Q.ToLower();
            query = query.Where(tg => tg.NameEn.ToLower().Contains(filter) ||
                                    tg.NameEs.ToLower().Contains(filter) ||
                                    tg.Slug.Contains(filter));
        }

        // 3. Подсчет общего количества
        var totalCount = await query.CountAsync(ct);

        // 4. Динамическая сортировка
        var isDescending = parameters._order?.ToUpper() == "DESC";
        query = parameters._sort switch
        {
            "NameEs" => isDescending ? query.OrderByDescending(tg => tg.NameEs) : query.OrderBy(tg => tg.NameEs),
            "Slug" => isDescending ? query.OrderByDescending(tg => tg.Slug) : query.OrderBy(tg => tg.Slug),
            _ => isDescending ? query.OrderByDescending(tg => tg.NameEn) : query.OrderBy(tg => tg.NameEn)
        };

        // 5. Пагинация
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 10;
        var take = end - start;

        var items = await query
            .Skip(start)
            .Take(take > 0 ? take : 10)
            .Select(tg => MapToDto(tg))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<TagGroupResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tg = await db.TagGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return tg == null ? null : MapToDto(tg);
    }

    public async Task<TagGroupResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var tg = await db.TagGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug.ToLowerInvariant(), ct);

        return tg == null ? null : MapToDto(tg);
    }

    public async Task<TagGroupResponseDto?> CreateAsync(TagGroupUpsertDto dto, CancellationToken ct = default)
    {
        var slugLower = dto.Slug.ToLowerInvariant();

        if (await db.TagGroups.AnyAsync(tg => tg.Slug == slugLower, ct))
            return null;

        var tagGroup = new TagGroup
        {
            Id = Guid.NewGuid(),
            NameEn = dto.NameEn,
            NameEs = dto.NameEs,
            Slug = slugLower
        };

        db.TagGroups.Add(tagGroup);
        await db.SaveChangesAsync(ct);

        return MapToDto(tagGroup);
    }

    public async Task<TagGroupResponseDto?> UpdateAsync(Guid id, TagGroupUpsertDto dto, CancellationToken ct = default)
    {
        var tagGroup = await db.TagGroups.FindAsync([id], ct);
        if (tagGroup == null) return null;

        var slugLower = dto.Slug.ToLowerInvariant();

        if (tagGroup.Slug != slugLower && await db.TagGroups.AnyAsync(tg => tg.Slug == slugLower, ct))
            return null;

        tagGroup.NameEn = dto.NameEn;
        tagGroup.NameEs = dto.NameEs;
        tagGroup.Slug = slugLower;

        await db.SaveChangesAsync(ct);
        return MapToDto(tagGroup);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tagGroup = await db.TagGroups
            .Include(tg => tg.Tags)
            .FirstOrDefaultAsync(tg => tg.Id == id, ct);

        if (tagGroup == null) return false;

        if (tagGroup.Tags.Any()) return false;

        db.TagGroups.Remove(tagGroup);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TagGroupResponseDto MapToDto(TagGroup tg) =>
        new(tg.Id, tg.NameEn, tg.NameEs, tg.Slug);
}