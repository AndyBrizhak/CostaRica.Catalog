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
            .AsQueryable();

        if (parameters.TagGroupId.HasValue)
        {
            query = query.Where(t => t.TagGroupId == parameters.TagGroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.NameEn))
        {
            var filter = parameters.NameEn.ToLower();
            query = query.Where(t => t.NameEn.ToLower().Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Slug))
        {
            var filter = parameters.Slug.ToLower();
            query = query.Where(t => t.Slug.Contains(filter));
        }

        var totalCount = await query.CountAsync(ct);

        // Обработка пагинации (стандарт react-admin)
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 10;

        query = query.OrderBy(t => t.NameEn)
                     .Skip(start)
                     .Take(end - start);

        var items = await query.ToListAsync(ct);
        return (items.Select(MapToDto), totalCount);
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
        // 1. Проверка существования родительской группы
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

        try
        {
            await db.SaveChangesAsync(ct);
            return MapToDto(tag);
        }
        catch (DbUpdateException)
        {
            return null;
        }
    }

    public async Task<TagResponseDto?> UpdateAsync(Guid id, TagUpsertDto dto, CancellationToken ct = default)
    {
        var tag = await db.Tags.FindAsync([id], ct);
        if (tag == null) return null;

        // Если группа меняется, проверяем её существование
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

        try
        {
            await db.SaveChangesAsync(ct);
            return MapToDto(tag);
        }
        catch (DbUpdateException)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await db.Tags.FindAsync([id], ct);
        if (tag == null) return false;

        db.Tags.Remove(tag);

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

    private static TagResponseDto MapToDto(Tag t) => new(
        t.Id,
        t.NameEn,
        t.NameEs,
        t.Slug,
        t.TagGroupId
    );
}