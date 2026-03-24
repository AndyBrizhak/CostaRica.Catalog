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
            .Include(b => b.SecondaryCategories) // Добавлено
            .AsQueryable();

        // 1. Фильтрация
        if (parameters.provinceId.HasValue)
            query = query.Where(b => b.ProvinceId == parameters.provinceId.Value);

        if (parameters.cityId.HasValue)
            query = query.Where(b => b.CityId == parameters.cityId.Value);

        if (parameters.isPublished.HasValue)
            query = query.Where(b => b.IsPublished == parameters.isPublished.Value);

        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var search = $"%{parameters.q}%";
            query = query.Where(b => EF.Functions.ILike(b.Name, search) ||
                                    EF.Functions.ILike(b.Slug, search));
        }

        // 2. Подсчет
        var totalCount = await query.CountAsync(ct);

        // 3. Сортировка
        if (!string.IsNullOrWhiteSpace(parameters._sort))
        {
            var isAsc = parameters._order?.ToUpper() == "ASC";
            query = parameters._sort.ToLower() switch
            {
                "name" => isAsc ? query.OrderBy(b => b.Name) : query.OrderByDescending(b => b.Name),
                "createdat" => isAsc ? query.OrderBy(b => b.CreatedAt) : query.OrderByDescending(b => b.CreatedAt),
                _ => query.OrderBy(b => b.Name)
            };
        }
        else
        {
            query = query.OrderBy(b => b.Name);
        }

        // 4. Пагинация
        if (parameters._start.HasValue && parameters._end.HasValue)
        {
            var take = parameters._end.Value - parameters._start.Value;
            query = query.Skip(parameters._start.Value).Take(take);
        }

        var items = await query
            .Include(b => b.Tags)
            .Include(b => b.Media)
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
            .Include(b => b.SecondaryCategories) // Добавлено
            .Include(b => b.Tags)
            .Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return business != null ? MapToDto(business) : null;
    }

    public async Task<BusinessPageResponseDto?> CreateAsync(BusinessPageUpsertDto dto, CancellationToken ct = default)
    {
        var slug = string.IsNullOrWhiteSpace(dto.Slug)
            ? GenerateSlug(dto.Name)
            : dto.Slug.ToLowerInvariant();

        if (await db.BusinessPages.AnyAsync(b => b.Slug == slug || b.OldSlugs.Contains(slug), ct))
        {
            logger.LogWarning("Slug already exists: {Slug}", slug);
            return null;
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

        if (dto.TagIds.Any())
            business.Tags = await db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct);

        if (dto.MediaIds.Any())
            business.Media = await db.MediaAssets.Where(m => dto.MediaIds.Contains(m.Id)).ToListAsync(ct);

        if (dto.SecondaryCategoryIds.Any())
            business.SecondaryCategories = await db.GoogleCategories.Where(c => dto.SecondaryCategoryIds.Contains(c.Id)).ToListAsync(ct);

        db.BusinessPages.Add(business);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(business.Id, ct);
    }

    public async Task<BusinessPageResponseDto?> UpdateAsync(Guid id, BusinessPageUpsertDto dto, CancellationToken ct = default)
    {
        var business = await db.BusinessPages
            .Include(b => b.Tags)
            .Include(b => b.Media)
            .Include(b => b.SecondaryCategories)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (business == null) return null;

        var newSlug = string.IsNullOrWhiteSpace(dto.Slug)
            ? GenerateSlug(dto.Name)
            : dto.Slug.ToLowerInvariant();

        if (business.Slug != newSlug)
        {
            if (await db.BusinessPages.AnyAsync(b => b.Id != id && (b.Slug == newSlug || b.OldSlugs.Contains(newSlug)), ct))
                return null;

            if (!business.OldSlugs.Contains(business.Slug))
                business.OldSlugs.Add(business.Slug);

            business.Slug = newSlug;
        }

        business.Name = dto.Name;
        business.IsPublished = dto.IsPublished;
        business.LanguageCode = dto.LanguageCode;
        business.Description = dto.Description;
        business.ProvinceId = dto.ProvinceId;
        business.CityId = dto.CityId;
        business.PrimaryCategoryId = dto.PrimaryCategoryId;
        business.Location = _geometryFactory.CreatePoint(new Coordinate(dto.Location.Longitude, dto.Location.Latitude));
        business.Contacts = dto.Contacts;
        business.Schedule = dto.Schedule;
        business.Seo = dto.Seo;
        business.UpdatedAt = DateTimeOffset.UtcNow;

        business.Tags.Clear();
        business.Tags = await db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct);

        business.Media.Clear();
        business.Media = await db.MediaAssets.Where(m => dto.MediaIds.Contains(m.Id)).ToListAsync(ct);

        business.SecondaryCategories.Clear();
        business.SecondaryCategories = await db.GoogleCategories.Where(c => dto.SecondaryCategoryIds.Contains(c.Id)).ToListAsync(ct);

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
            .Where(c => char.IsLetterOrDigit(c) || c == '-').Aggregate("", (s, c) => s + c);
    }

    private static BusinessPageResponseDto MapToDto(BusinessPage b) => new(
        b.Id,
        b.IsPublished,
        b.Name,
        b.Slug,
        b.OldSlugs,
        b.LanguageCode,
        b.Description,
        b.ProvinceId,
        b.Province?.Name,
        b.CityId,
        b.City?.Name,
        new GeoPointDto(b.Location.Y, b.Location.X),
        b.PrimaryCategoryId,
        b.PrimaryCategory?.NameEn,
        b.SecondaryCategories.Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs)), // Новое
        b.Contacts,
        b.Schedule,
        b.Seo,
        b.Tags.Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId)),
        b.Media.Select(m => new MediaAssetResponseDto(
            m.Id,
            m.Slug,
            m.FileName,
            m.ContentType,
            m.AltTextEn,
            m.AltTextEs,
            $"/media/{m.FileName}",
            m.CreatedAt,
            new List<Guid> { b.Id }
        )),
        b.CreatedAt,
        b.UpdatedAt
    );
}