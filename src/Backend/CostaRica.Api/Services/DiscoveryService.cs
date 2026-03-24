using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CostaRica.Api.Services;

public class DiscoveryService(DirectoryDbContext db) : IDiscoveryService
{
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    public async Task<(IEnumerable<BusinessPageCardDto> Items, int TotalCount)> SearchAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default)
    {
        var query = db.BusinessPages
            .AsNoTracking()
            .Where(b => b.IsPublished);

        // 1. ПРИМЕНЕНИЕ ФИЛЬТРОВ
        query = ApplyDiscoveryFilters(query, @params);

        // 2. ГЕО-ФИЛЬТРАЦИЯ (ST_DWithin)
        Point? userPoint = null;
        if (@params.Lat.HasValue && @params.Lon.HasValue)
        {
            userPoint = _geometryFactory.CreatePoint(new Coordinate(@params.Lon.Value, @params.Lat.Value));

            if (@params.RadiusInKm.HasValue)
            {
                var radiusMeters = @params.RadiusInKm.Value * 1000;
                query = query.Where(b => b.Location.Distance(userPoint) <= radiusMeters);
            }
        }

        var totalCount = await query.CountAsync(ct);

        // 3. МАППИНГ И СОРТИРОВКА
        // Исправлено: Явно указываем IQueryable<BusinessPage>, чтобы избежать конфликта типов Include/OrderBy
        IQueryable<BusinessPage> itemsQuery = query
            .Include(b => b.Province)
            .Include(b => b.City)
            .Include(b => b.PrimaryCategory)
            .Include(b => b.Media);

        if (userPoint != null)
        {
            itemsQuery = itemsQuery.OrderBy(b => b.Location.Distance(userPoint));
        }
        else
        {
            itemsQuery = itemsQuery.OrderBy(b => b.Name);
        }

        // 4. ПАГИНАЦИЯ
        var skip = (@params.Page - 1) * @params.PageSize;
        var items = await itemsQuery
            .Skip(skip)
            .Take(@params.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(b => new BusinessPageCardDto(
            b.Name,
            b.Slug,
            b.Media.FirstOrDefault()?.FileName != null ? $"/media/{b.Media.First().FileName}" : null,
            b.City?.Name,
            b.Province?.Name,
            b.PrimaryCategory?.NameEn,
            new GeoPointDto(b.Location.Y, b.Location.X),
            userPoint != null ? b.Location.Distance(userPoint) / 1000 : null
        ));

        return (dtos, totalCount);
    }

    public async Task<IEnumerable<ProvinceResponseDto>> GetAvailableProvincesAsync(DiscoverySearchParams @params, CancellationToken ct = default)
    {
        var query = db.BusinessPages.AsNoTracking().Where(b => b.IsPublished);

        var filteredParams = @params with { ProvinceId = null, CityId = null };
        query = ApplyDiscoveryFilters(query, filteredParams);
        query = ApplyGeoFilter(query, filteredParams);

        return await query
            .Select(b => b.Province)
            .Where(p => p != null)
            .Distinct()
            .Select(p => new ProvinceResponseDto(p!.Id, p.Name, p.Slug, null))
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<CityResponseDto>> GetAvailableCitiesAsync(DiscoverySearchParams @params, CancellationToken ct = default)
    {
        var query = db.BusinessPages.AsNoTracking().Where(b => b.IsPublished);

        query = ApplyDiscoveryFilters(query, @params);
        query = ApplyGeoFilter(query, @params);

        return await query
            .Select(b => b.City)
            .Where(c => c != null)
            .Distinct()
            .Select(c => new CityResponseDto(c!.Id, c.Name, c.Slug, c.ProvinceId, null))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TagResponseDto>> GetAvailableTagsAsync(DiscoverySearchParams @params, CancellationToken ct = default)
    {
        var query = db.BusinessPages.AsNoTracking().Where(b => b.IsPublished);

        var filteredParams = @params with { TagIds = null };
        query = ApplyDiscoveryFilters(query, filteredParams);
        query = ApplyGeoFilter(query, filteredParams);

        return await query
            .SelectMany(b => b.Tags)
            .Distinct()
            .Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId))
            .OrderBy(t => t.NameEn)
            .ToListAsync(ct);
    }

    public async Task<BusinessPageResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var business = await db.BusinessPages
            .AsNoTracking()
            .Include(b => b.Province)
            .Include(b => b.City)
            .Include(b => b.PrimaryCategory)
            .Include(b => b.SecondaryCategories)
            .Include(b => b.Tags)
            .Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Slug == slug || b.OldSlugs.Contains(slug), ct);

        return business != null ? MapToFullDto(business) : null;
    }

    private IQueryable<BusinessPage> ApplyDiscoveryFilters(IQueryable<BusinessPage> query, DiscoverySearchParams p)
    {
        if (p.ProvinceId.HasValue)
            query = query.Where(b => b.ProvinceId == p.ProvinceId.Value);

        if (p.CityId.HasValue)
            query = query.Where(b => b.CityId == p.CityId.Value);

        if (p.TagIds != null && p.TagIds.Any())
        {
            foreach (var tagId in p.TagIds)
            {
                query = query.Where(b => b.Tags.Any(t => t.Id == tagId));
            }
        }

        return query;
    }

    private IQueryable<BusinessPage> ApplyGeoFilter(IQueryable<BusinessPage> query, DiscoverySearchParams p)
    {
        if (p.Lat.HasValue && p.Lon.HasValue && p.RadiusInKm.HasValue)
        {
            var userPoint = _geometryFactory.CreatePoint(new Coordinate(p.Lon.Value, p.Lat.Value));
            var radiusMeters = p.RadiusInKm.Value * 1000;
            query = query.Where(b => b.Location.Distance(userPoint) <= radiusMeters);
        }
        return query;
    }

    private static BusinessPageResponseDto MapToFullDto(BusinessPage b) => new(
        b.Id, b.IsPublished, b.Name, b.Slug, b.OldSlugs, b.LanguageCode, b.Description,
        b.ProvinceId, b.Province?.Name, b.CityId, b.City?.Name,
        new GeoPointDto(b.Location.Y, b.Location.X),
        b.PrimaryCategoryId, b.PrimaryCategory?.NameEn,
        b.SecondaryCategories.Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs)),
        b.Contacts, b.Schedule, b.Seo,
        b.Tags.Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId)),
        b.Media.Select(m => new MediaAssetResponseDto(m.Id, m.Slug, m.FileName, m.ContentType, m.AltTextEn, m.AltTextEs, $"/media/{m.FileName}", m.CreatedAt, new List<Guid> { b.Id })),
        b.CreatedAt, b.UpdatedAt
    );
}