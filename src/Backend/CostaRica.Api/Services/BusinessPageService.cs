using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CostaRica.Api.Services;

public class BusinessPageService(DirectoryDbContext db, ILogger<BusinessPageService> logger) : IBusinessPageService
{
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    public async Task<(IEnumerable<BusinessPageResponseDto> Items, int TotalCount)> GetAllAsync(
        BusinessPageQueryParameters parameters,
        CancellationToken ct = default)
    {
        var query = db.BusinessPages
            .AsNoTracking()
            .Include(b => b.Province)
            .Include(b => b.City)
            .Include(b => b.PrimaryCategory)
            .Include(b => b.SecondaryCategories)
            .AsQueryable();

        if (parameters.provinceId.HasValue)
            query = query.Where(b => b.ProvinceId == parameters.provinceId.Value);

        if (parameters.cityId.HasValue)
            query = query.Where(b => b.CityId == parameters.cityId.Value);

        if (parameters.isPublished.HasValue)
            query = query.Where(b => b.IsPublished == parameters.isPublished.Value);

        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var search = $"%{parameters.q}%";
            query = query.Where(b => EF.Functions.ILike(b.Name, search) || (b.Description != null && EF.Functions.ILike(b.Description, search)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(b => b.Name)
            .Skip(parameters._start ?? 0)
            .Take((parameters._end ?? 10) - (parameters._start ?? 0))
            .ToListAsync(ct);

        return (items.Select(MapToDto), totalCount);
    }

    public async Task<BusinessPageResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var business = await db.BusinessPages
            .AsNoTracking()
            .Include(b => b.Province)
            .Include(b => b.City)
            .Include(b => b.PrimaryCategory)
            .Include(b => b.SecondaryCategories)
            .Include(b => b.Tags)
            .Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return business is not null ? MapToDto(business) : null;
    }

    public async Task<BusinessPageResponseDto?> CreateAsync(BusinessPageUpsertDto dto, CancellationToken ct = default)
    {
        var slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug;

        if (await db.BusinessPages.AnyAsync(b => b.Slug == slug, ct))
        {
            slug = $"{slug}-{Guid.NewGuid().ToString()[..4]}";
        }

        var business = new BusinessPage
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = slug,
            IsPublished = dto.IsPublished,
            LanguageCode = dto.LanguageCode,
            Description = dto.Description,
            ProvinceId = dto.ProvinceId,
            CityId = dto.CityId,
            PrimaryCategoryId = dto.PrimaryCategoryId,
            Location = _geometryFactory.CreatePoint(new Coordinate(dto.Location.Longitude, dto.Location.Latitude)),
            Contacts = dto.Contacts,
            Schedule = dto.Schedule,
            Seo = dto.Seo,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        if (dto.SecondaryCategoryIds?.Any() == true)
            business.SecondaryCategories = await db.GoogleCategories.Where(c => dto.SecondaryCategoryIds.Contains(c.Id)).ToListAsync(ct);

        if (dto.TagIds?.Any() == true)
            business.Tags = await db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct);

        if (dto.MediaIds?.Any() == true)
            business.Media = await db.MediaAssets.Where(m => dto.MediaIds.Contains(m.Id)).ToListAsync(ct);

        db.BusinessPages.Add(business);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(business.Id, ct);
    }

    public async Task<BusinessPageResponseDto?> UpdateAsync(Guid id, BusinessPageUpsertDto dto, CancellationToken ct = default)
    {
        var business = await db.BusinessPages
            .Include(b => b.SecondaryCategories)
            .Include(b => b.Tags)
            .Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (business == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Slug) && business.Slug != dto.Slug)
        {
            var isSlugTaken = await db.BusinessPages.AnyAsync(b => b.Slug == dto.Slug && b.Id != id, ct);
            if (!isSlugTaken)
            {
                if (!business.OldSlugs.Contains(business.Slug))
                    business.OldSlugs.Add(business.Slug);

                business.Slug = dto.Slug;
            }
        }

        business.Name = dto.Name;
        business.IsPublished = dto.IsPublished;
        business.LanguageCode = dto.LanguageCode;
        business.Description = dto.Description;
        business.UpdatedAt = DateTimeOffset.UtcNow;

        if (business.ProvinceId != dto.ProvinceId)
        {
            if (await db.Provinces.AnyAsync(p => p.Id == dto.ProvinceId, ct))
                business.ProvinceId = dto.ProvinceId;
        }

        if (dto.CityId.HasValue)
        {
            if (await db.Cities.AnyAsync(c => c.Id == dto.CityId.Value, ct))
                business.CityId = dto.CityId;
        }
        else business.CityId = null;

        if (dto.PrimaryCategoryId.HasValue)
        {
            if (await db.GoogleCategories.AnyAsync(c => c.Id == dto.PrimaryCategoryId.Value, ct))
                business.PrimaryCategoryId = dto.PrimaryCategoryId;
        }
        else business.PrimaryCategoryId = null;

        business.Location = _geometryFactory.CreatePoint(new Coordinate(dto.Location.Longitude, dto.Location.Latitude));

        business.Contacts = dto.Contacts;
        business.Schedule = dto.Schedule;
        business.Seo = dto.Seo;

        business.SecondaryCategories.Clear();
        if (dto.SecondaryCategoryIds?.Any() == true)
        {
            var cats = await db.GoogleCategories.Where(c => dto.SecondaryCategoryIds.Contains(c.Id)).ToListAsync(ct);
            foreach (var cat in cats) business.SecondaryCategories.Add(cat);
        }

        business.Tags.Clear();
        if (dto.TagIds?.Any() == true)
        {
            var tags = await db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var tag in tags) business.Tags.Add(tag);
        }

        business.Media.Clear();
        if (dto.MediaIds?.Any() == true)
        {
            var media = await db.MediaAssets.Where(m => dto.MediaIds.Contains(m.Id)).ToListAsync(ct);
            foreach (var m in media) business.Media.Add(m);
        }

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(business.Id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var business = await db.BusinessPages.FindAsync([id], ct);
        if (business == null) return false;

        db.BusinessPages.Remove(business);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate("", (s, c) => s + c);
    }

    private static BusinessPageResponseDto MapToDto(BusinessPage b) => new(
        b.Id, b.IsPublished, b.Name, b.Slug, b.OldSlugs, b.LanguageCode, b.Description,
        b.ProvinceId, b.Province?.Name, b.CityId, b.City?.Name,
        new GeoPointDto(b.Location.Y, b.Location.X),
        b.PrimaryCategoryId, b.PrimaryCategory?.NameEn,
        b.SecondaryCategories.Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs)),
        b.Contacts, b.Schedule, b.Seo,

        // ИСПРАВЛЕНО: передаем t.TagGroupId вместо null
        b.Tags.Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId)),

        b.Media.Select(m => new MediaAssetResponseDto(
            m.Id,
            m.Slug,
            m.FileName,
            m.ContentType,
            m.AltTextEn,
            m.AltTextEs,
            $"/media/{m.Id}/{m.Slug}",
            m.CreatedAt,
            m.BusinessPages.Select(bp => bp.Id)
        )),

        b.CreatedAt, b.UpdatedAt
    );
}