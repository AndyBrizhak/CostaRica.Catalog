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
            .Select(tg => MapToDto(tg))
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
            .FirstOrDefaultAsync(x => x.Slug == slug.ToLowerInvariant(), ct);

        return tg == null ? null : MapToDto(tg);
    }

    public async Task<TagGroupResponseDto?> CreateAsync(TagGroupUpsertDto dto, CancellationToken ct = default)
    {
        var slugLower = dto.Slug.ToLowerInvariant();

        // Проверка уникальности слага: если уже существует, возвращаем null
        if (await db.TagGroups.AnyAsync(tg => tg.Slug == slugLower, ct))
        {
            return null;
        }

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

        // Если слаг меняется, проверяем, не занят ли новый слаг другой группой
        if (tagGroup.Slug != slugLower && await db.TagGroups.AnyAsync(tg => tg.Slug == slugLower, ct))
        {
            return null;
        }

        tagGroup.NameEn = dto.NameEn;
        tagGroup.NameEs = dto.NameEs;
        tagGroup.Slug = slugLower;

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