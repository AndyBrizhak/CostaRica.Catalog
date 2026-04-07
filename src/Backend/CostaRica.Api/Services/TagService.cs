using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class TagService(DirectoryDbContext db) : ITagService
{
    public async Task<(IEnumerable<TagResponseDto> Items, int TotalCount)> GetAllAsync(TagQueryParameters parameters, CancellationToken ct = default)
    {
        var query = db.Tags
            .AsNoTracking()
            .Include(t => t.TagGroup)
            .AsQueryable();

        // 1. Фильтрация по группе
        if (parameters.TagGroupId.HasValue && parameters.TagGroupId != Guid.Empty)
        {
            query = query.Where(t => t.TagGroupId == parameters.TagGroupId.Value);
        }

        // 2. Глобальный поиск (Q)
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            var search = $"%{parameters.Q}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.NameEn, search) ||
                EF.Functions.ILike(t.NameEs, search) ||
                EF.Functions.ILike(t.Slug, search));
        }

        var totalCount = await query.CountAsync(ct);

        // 3. Динамическая сортировка
        var isDescending = string.Equals(parameters._order, "DESC", StringComparison.OrdinalIgnoreCase);
        var sortField = parameters._sort?.ToLowerInvariant() ?? "nameen";

        query = sortField switch
        {
            "nameen" => isDescending ? query.OrderByDescending(t => t.NameEn) : query.OrderBy(t => t.NameEn),
            "namees" => isDescending ? query.OrderByDescending(t => t.NameEs) : query.OrderBy(t => t.NameEs),
            "slug" => isDescending ? query.OrderByDescending(t => t.Slug) : query.OrderBy(t => t.Slug),
            "taggroupname" => isDescending ? query.OrderByDescending(t => t.TagGroup!.NameEn) : query.OrderBy(t => t.TagGroup!.NameEn),
            _ => query.OrderBy(t => t.NameEn)
        };

        // 4. Пагинация
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 10;
        var take = end - start;
        if (take < 0) take = 0;

        var items = await query
            .Skip(start)
            .Take(take)
            .Select(t => MapToDto(t))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<TagResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await db.Tags
            .AsNoTracking()
            .Include(t => t.TagGroup)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return tag != null ? MapToDto(tag) : null;
    }

    public async Task<TagResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var tag = await db.Tags
            .AsNoTracking()
            .Include(t => t.TagGroup)
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

        return tag != null ? MapToDto(tag) : null;
    }

    public async Task<TagResponseDto?> CreateAsync(TagUpsertDto dto, CancellationToken ct = default)
    {
        var groupExists = await db.TagGroups.AnyAsync(tg => tg.Id == dto.TagGroupId, ct);
        if (!groupExists) return null;

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

        return await GetByIdAsync(tag.Id, ct);
    }

    public async Task<TagResponseDto?> UpdateAsync(Guid id, TagUpsertDto dto, CancellationToken ct = default)
    {
        var tag = await db.Tags.FindAsync([id], ct);
        if (tag == null) return null;

        if (tag.TagGroupId != dto.TagGroupId)
        {
            var groupExists = await db.TagGroups.AnyAsync(tg => tg.Id == dto.TagGroupId, ct);
            if (!groupExists) return null;
        }

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
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await db.Tags.FindAsync([id], ct);
        if (tag == null) return false;

        // ПРОВЕРКА ЗАВИСИМОСТЕЙ:
        // Проверяем, привязан ли тег к бизнес-страницам через навигационное свойство.
        // Мы используем AnyAsync в коллекции Tags сущности BusinessPage.
        var isUsed = await db.BusinessPages.AnyAsync(bp => bp.Tags.Any(t => t.Id == id), ct);
        if (isUsed)
        {
            // Возвращаем false. В текущем эндпоинте это вызовет NotFound, 
            // что защитит БД от ошибки Foreign Key Violation.
            return false;
        }

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TagResponseDto MapToDto(Tag tag) =>
        new(tag.Id, tag.NameEn, tag.NameEs, tag.Slug, tag.TagGroupId, tag.TagGroup?.NameEn);
}