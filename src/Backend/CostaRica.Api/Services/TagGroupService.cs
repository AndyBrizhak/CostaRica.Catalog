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

        // 2. Сортировка
        var sortField = parameters._sort?.ToLower() ?? "nameen";
        var isAscending = parameters._order?.ToUpper() == "ASC";

        query = sortField switch
        {
            "namees" => isAscending ? query.OrderBy(tg => tg.NameEs) : query.OrderByDescending(tg => tg.NameEs),
            "slug" => isAscending ? query.OrderBy(tg => tg.Slug) : query.OrderByDescending(tg => tg.Slug),
            _ => isAscending ? query.OrderBy(tg => tg.NameEn) : query.OrderByDescending(tg => tg.NameEn)
        };

        // 3. Пагинация
        var skip = parameters._start ?? 0;
        var take = (parameters._end ?? 9) - skip + 1;

        var items = await query
            .Skip(skip)
            .Take(take)
            .Select(tg => new TagGroupResponseDto(tg.Id, tg.NameEn, tg.NameEs, tg.Slug))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<TagGroupResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tg = await db.TagGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return tg != null ? new TagGroupResponseDto(tg.Id, tg.NameEn, tg.NameEs, tg.Slug) : null;
    }

    public async Task<TagGroupResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var tg = await db.TagGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, ct);
        return tg != null ? new TagGroupResponseDto(tg.Id, tg.NameEn, tg.NameEs, tg.Slug) : null;
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

    public async Task<TagGroupDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var group = await db.TagGroups
            .Include(tg => tg.Tags)
            .FirstOrDefaultAsync(tg => tg.Id == id, ct);

        if (group == null)
            return TagGroupDeleteResult.NotFound;

        if (group.Tags.Any())
            return TagGroupDeleteResult.InUse;

        db.TagGroups.Remove(group);
        await db.SaveChangesAsync(ct);

        return TagGroupDeleteResult.Success;
    }
}