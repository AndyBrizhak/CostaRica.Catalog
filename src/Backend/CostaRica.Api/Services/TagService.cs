using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class TagService(DirectoryDbContext db) : ITagService
{
    public async Task<IEnumerable<TagResponseDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Tags
            .AsNoTracking()
            .Select(t => MapToDto(t))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TagResponseDto>> GetByGroupIdAsync(Guid groupId, CancellationToken ct = default)
    {
        return await db.Tags
            .AsNoTracking()
            .Where(t => t.TagGroupId == groupId)
            .Select(t => MapToDto(t))
            .ToListAsync(ct);
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
        var slugLower = dto.Slug.ToLowerInvariant();

        // 1. Проверяем существование группы
        var groupExists = await db.TagGroups.AnyAsync(tg => tg.Id == dto.TagGroupId, ct);
        if (!groupExists) return null;

        // 2. Проверяем уникальность слага
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

        var slugLower = dto.Slug.ToLowerInvariant();

        // 1. Если группа меняется, проверяем её наличие
        if (tag.TagGroupId != dto.TagGroupId)
        {
            var groupExists = await db.TagGroups.AnyAsync(tg => tg.Id == dto.TagGroupId, ct);
            if (!groupExists) return null;
        }

        // 2. Если слаг меняется, проверяем уникальность
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