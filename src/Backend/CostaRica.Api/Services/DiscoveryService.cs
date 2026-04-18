using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CostaRica.Api.Services;

public class DiscoveryService(
    DirectoryDbContext db,
    ILogger<DiscoveryService> logger) : IDiscoveryService
{
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    public async Task<(IEnumerable<BusinessPageCardDto> Items, int TotalCount)> SearchAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default)
    {
        var query = db.BusinessPages.AsNoTracking().Where(b => b.IsPublished);
        query = ApplyDiscoveryFilters(query, @params);

        Point? userPoint = null;
        if (@params.Lat.HasValue && @params.Lon.HasValue)
            userPoint = _geometryFactory.CreatePoint(new Coordinate(@params.Lon.Value, @params.Lat.Value));

        var totalCount = await query.CountAsync(ct);
        query = query.OrderByDescending(b => b.CreatedAt)
                     .Skip((@params.Page - 1) * @params.PageSize)
                     .Take(@params.PageSize);

        var items = await query.Include(b => b.City).Include(b => b.Province)
                               .Include(b => b.PrimaryCategory).Include(b => b.Media)
                               .ToListAsync(ct);

        return (items.Select(b => MapToCardDto(b, userPoint)), totalCount);
    }

    public async Task<BusinessPageDiscoveryDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var business = await db.BusinessPages.AsNoTracking()
            .Include(b => b.Province).Include(b => b.City)
            .Include(b => b.PrimaryCategory).Include(b => b.SecondaryCategories)
            .Include(b => b.Tags).ThenInclude(t => t.TagGroup)
            .Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished, ct);

        if (business == null)
        {
            logger.LogWarning("Slug {Slug} not found.", slug);
            return null;
        }

        return MapToDiscoveryDto(business);
    }

    public async Task<IEnumerable<ProvinceResponseDto>> GetAvailableProvincesAsync(DiscoverySearchParams @params, CancellationToken ct = default)
    {
        var query = db.BusinessPages.AsNoTracking().Where(b => b.IsPublished);
        query = ApplyDiscoveryFilters(query, @params);
        return await query.Select(b => b.Province!).Distinct().Select(p => new ProvinceResponseDto(p.Id, p.Name, p.Slug, null)).ToListAsync(ct);
    }

    public async Task<IEnumerable<CityResponseDto>> GetAvailableCitiesAsync(DiscoverySearchParams @params, CancellationToken ct = default)
    {
        var query = db.BusinessPages.AsNoTracking().Where(b => b.IsPublished);
        query = ApplyDiscoveryFilters(query, @params);
        return await query.Where(b => b.CityId != null).Select(b => b.City!).Distinct().Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId, null)).ToListAsync(ct);
    }

    public async Task<IEnumerable<TagResponseDto>> GetAvailableTagsAsync(DiscoverySearchParams @params, CancellationToken ct = default)
    {
        var query = db.BusinessPages.AsNoTracking().Where(b => b.IsPublished);
        query = ApplyDiscoveryFilters(query, @params);
        return await query.SelectMany(b => b.Tags).Distinct().Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId, null)).ToListAsync(ct);
    }

    private IQueryable<BusinessPage> ApplyDiscoveryFilters(IQueryable<BusinessPage> query, DiscoverySearchParams p)
    {
        if (!string.IsNullOrWhiteSpace(p.Q))
        {
            var search = $"%{p.Q}%";
            query = query.Where(b => EF.Functions.ILike(b.Name, search) || EF.Functions.ILike(b.Slug, search));
        }

        if (p.ProvinceId.HasValue) query = query.Where(b => b.ProvinceId == p.ProvinceId.Value);
        if (p.CityId.HasValue) query = query.Where(b => b.CityId == p.CityId.Value);
        if (p.CategoryId.HasValue) query = query.Where(b => b.PrimaryCategoryId == p.CategoryId.Value || b.SecondaryCategories.Any(c => c.Id == p.CategoryId.Value));

        // Исправлено: работа с массивом TagIds
        if (p.TagIds is { Length: > 0 })
            query = query.Where(b => b.Tags.Any(t => p.TagIds.Contains(t.Id)));

        if (p.Lat.HasValue && p.Lon.HasValue && p.RadiusInKm.HasValue)
        {
            var userPoint = _geometryFactory.CreatePoint(new Coordinate(p.Lon.Value, p.Lat.Value));
            query = query.Where(b => EF.Functions.IsWithinDistance(b.Location, userPoint, p.RadiusInKm.Value * 1000, true));
        }

        return query;
    }

    // Методы маппинга MapToCardDto и MapToDiscoveryDto остаются прежними (из моей предыдущей выдачи)
    private static BusinessPageCardDto MapToCardDto(BusinessPage b, Point? userPoint) => new(
        b.Id, b.Name, b.Slug, b.Media.FirstOrDefault()?.FileName, b.City?.Name, b.Province?.Name, b.PrimaryCategory?.NameEn,
        new GeoPointDto(b.Location.Y, b.Location.X),
        userPoint != null ? b.Location.Distance(userPoint) / 1000 : null);

    private static BusinessPageDiscoveryDto MapToDiscoveryDto(BusinessPage b) => new(
        b.Id, b.Name, b.Slug, b.LanguageCode, b.Description, b.Province?.Name, b.City?.Name,
        new GeoPointDto(b.Location.Y, b.Location.X), b.PrimaryCategory?.NameEn,
        b.SecondaryCategories.Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs)),
        b.Contacts, b.Schedule, b.Seo,
        b.Tags.Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId, t.TagGroup?.NameEn)),
        b.Media.Select(m => new MediaAssetResponseDto(m.Id, m.Slug, m.FileName, m.ContentType, m.AltTextEn, m.AltTextEs, "", m.CreatedAt, [])));
}