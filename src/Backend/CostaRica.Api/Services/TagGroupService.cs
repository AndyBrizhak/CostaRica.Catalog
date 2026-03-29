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

        if (!string.IsNullOrWhiteSpace(parameters.NameEn))
        {
            var filter = parameters.NameEn.ToLower();
            query = query.Where(tg => tg.NameEn.ToLower().Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Slug))
        {
            var filter = parameters.Slug.ToLower();
            query = query.Where(tg => tg.Slug.Contains(filter));
        }

        var totalCount = await query.CountAsync(ct);

        var start = parameters._start ?? 0;
        var end = parameters._end ?? 10;

        query = query.OrderBy(tg => tg.NameEn)
                     .Skip(start)
                     .Take(end - start);

        var items = await query.ToListAsync(ct);
        return (items.Select(MapToDto), totalCount);
    }

    public async Task<TagGroupResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tagGroup = await db.TagGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(tg => tg.Id == id, ct);

        return tagGroup == null ? null : MapToDto(tagGroup);
    }

    public async Task<TagGroupResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var tagGroup = await db.TagGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(tg => tg.Slug == slug.ToLowerInvariant(), ct);

        return tagGroup == null ? null : MapToDto(tagGroup);
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

        try
        {
            await db.SaveChangesAsync(ct);
            return MapToDto(tagGroup);
        }
        catch (DbUpdateException)
        {
            return null;
        }
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

        try
        {
            await db.SaveChangesAsync(ct);
            return MapToDto(tagGroup);
        }
        catch (DbUpdateException)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // Включаем теги, чтобы проверить зависимость
        var tagGroup = await db.TagGroups
            .Include(tg => tg.Tags)
            .FirstOrDefaultAsync(tg => tg.Id == id, ct);

        if (tagGroup == null) return false;

        // Если есть связанные теги, удаление запрещено во избежание ошибки FK
        if (tagGroup.Tags.Any()) return false;

        db.TagGroups.Remove(tagGroup);

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    private static TagGroupResponseDto MapToDto(TagGroup tg) => new(
        tg.Id,
        tg.NameEn,
        tg.NameEs,
        tg.Slug
    );
}