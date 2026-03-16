using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

/// <summary>
/// Реализация сервиса управления тегами (стандарт react-admin).
/// </summary>
public class TagService(DirectoryDbContext db) : ITagService
{
    public async Task<(IEnumerable<TagResponseDto> Items, int TotalCount)> GetAllAsync(TagQueryParameters parameters, CancellationToken ct = default)
    {
        var query = db.Tags
            .AsNoTracking()
            .AsQueryable();

        // 1. Фильтрация по родительской группе (важно для react-admin ReferenceInput)
        if (parameters.TagGroupId.HasValue)
        {
            query = query.Where(t => t.TagGroupId == parameters.TagGroupId.Value);
        }

        // 2. Фильтрация по конкретным полям
        if (!string.IsNullOrWhiteSpace(parameters.NameEn))
        {
            query = query.Where(t => EF.Functions.ILike(t.NameEn, $"%{parameters.NameEn}%"));
        }

        if (!string.IsNullOrWhiteSpace(parameters.NameEs))
        {
            query = query.Where(t => EF.Functions.ILike(t.NameEs, $"%{parameters.NameEs}%"));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Slug))
        {
            query = query.Where(t => t.Slug.Contains(parameters.Slug.ToLowerInvariant()));
        }

        // 3. Глобальный поиск (Q)
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            query = query.Where(t => EF.Functions.ILike(t.NameEn, $"%{parameters.Q}%") ||
                                    EF.Functions.ILike(t.NameEs, $"%{parameters.Q}%") ||
                                    t.Slug.Contains(parameters.Q.ToLowerInvariant()));
        }

        // 4. Подсчет общего количества до пагинации
        var totalCount = await query.CountAsync(ct);

        // 5. Динамическая сортировка
        var isDescending = parameters._order?.ToUpper() == "DESC";
        query = parameters._sort switch
        {
            "NameEs" => isDescending ? query.OrderByDescending(t => t.NameEs) : query.OrderBy(t => t.NameEs),
            "Slug" => isDescending ? query.OrderByDescending(t => t.Slug) : query.OrderBy(t => t.Slug),
            "TagGroupId" => isDescending ? query.OrderByDescending(t => t.TagGroupId) : query.OrderBy(t => t.TagGroupId),
            _ => isDescending ? query.OrderByDescending(t => t.NameEn) : query.OrderBy(t => t.NameEn)
        };

        // 6. Пагинация
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 10;
        var take = end - start;

        var items = await query
            .Skip(start)
            .Take(take > 0 ? take : 10)
            .Select(t => MapToDto(t))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<TagResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await db.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return tag == null ? null : MapToDto(tag);
    }

    public async Task<TagResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var tag = await db.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

        return tag == null ? null : MapToDto(tag);
    }

    public async Task<TagResponseDto?> CreateAsync(TagUpsertDto dto, CancellationToken ct = default)
    {
        // 1. Проверка наличия родительской группы
        var groupExists = await db.TagGroups.AnyAsync(tg => tg.Id == dto.TagGroupId, ct);
        if (!groupExists) return null;

        // 2. Проверка уникальности слага
        var slugLower = dto.Slug.ToLowerInvariant();
        if (await db.Tags.AnyAsync(t => t.Slug == slugLower, ct)) return null;

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            NameEn = dto.NameEn,
            NameEs = dto.NameEs,
            Slug = slugLower,
            TagGroupId = dto.TagGroupId
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);

        return MapToDto(tag);
    }

    public async Task<TagResponseDto?> UpdateAsync(Guid id, TagUpsertDto dto, CancellationToken ct = default)
    {
        var tag = await db.Tags.FindAsync([id], ct);
        if (tag == null) return null;

        // 1. Проверка наличия родительской группы (если она изменилась)
        if (tag.TagGroupId != dto.TagGroupId)
        {
            var groupExists = await db.TagGroups.AnyAsync(tg => tg.Id == dto.TagGroupId, ct);
            if (!groupExists) return null;
        }

        // 2. Проверка уникальности слага
        var slugLower = dto.Slug.ToLowerInvariant();
        if (tag.Slug != slugLower && await db.Tags.AnyAsync(t => t.Slug == slugLower, ct))
        {
            return null;
        }

        tag.NameEn = dto.NameEn;
        tag.NameEs = dto.NameEs;
        tag.Slug = slugLower;
        tag.TagGroupId = dto.TagGroupId;

        await db.SaveChangesAsync(ct);
        return MapToDto(tag);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await db.Tags.FindAsync([id], ct);
        if (tag == null) return false;

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TagResponseDto MapToDto(Tag t) =>
        new(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId);
}