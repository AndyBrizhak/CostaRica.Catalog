using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class TagGroupService(DirectoryDbContext db) : ITagGroupService
{
    public async Task<IEnumerable<TagGroupResponseDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.TagGroups
            .AsNoTracking()
            .Select(tg => new TagGroupResponseDto(
                tg.Id,
                tg.NameEn,
                tg.NameEs,
                tg.Slug))
            .ToListAsync(ct);
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
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        return tg == null ? null : MapToDto(tg);
    }

    public async Task<TagGroupResponseDto> CreateAsync(TagGroupUpsertDto dto, CancellationToken ct = default)
    {
        var tagGroup = new TagGroup
        {
            Id = Guid.NewGuid(),
            NameEn = dto.NameEn,
            NameEs = dto.NameEs,
            Slug = dto.Slug.ToLowerInvariant()
        };

        db.TagGroups.Add(tagGroup);
        await db.SaveChangesAsync(ct);

        return MapToDto(tagGroup);
    }

    public async Task<TagGroupResponseDto?> UpdateAsync(Guid id, TagGroupUpsertDto dto, CancellationToken ct = default)
    {
        var tagGroup = await db.TagGroups.FindAsync([id], ct);
        if (tagGroup == null) return null;

        tagGroup.NameEn = dto.NameEn;
        tagGroup.NameEs = dto.NameEs;
        tagGroup.Slug = dto.Slug.ToLowerInvariant();

        await db.SaveChangesAsync(ct);
        return MapToDto(tagGroup);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tagGroup = await db.TagGroups.FindAsync([id], ct);
        if (tagGroup == null) return false;

        db.TagGroups.Remove(tagGroup);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TagGroupResponseDto MapToDto(TagGroup tg) =>
        new(tg.Id, tg.NameEn, tg.NameEs, tg.Slug);
}