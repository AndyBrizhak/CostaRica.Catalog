using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CostaRica.Api.Services;

/// <summary>
/// Реализация сервиса управления бизнес-страницами (Админ-панель).
/// Исправлено: внедрено использование логгера для устранения ошибок компиляции.
/// </summary>
public class BusinessPageService(
    DirectoryDbContext db,
    ILogger<BusinessPageService> logger) : IBusinessPageService
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
            .Include(b => b.Tags)
            .Include(b => b.Media)
            .AsQueryable();

        // 1. Поиск
        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var search = $"%{parameters.q}%";
            query = query.Where(b => EF.Functions.ILike(b.Name, search) || EF.Functions.ILike(b.Slug, search));
        }

        // 2. Фильтрация
        if (parameters.provinceId.HasValue) query = query.Where(b => b.ProvinceId == parameters.provinceId.Value);
        if (parameters.cityId.HasValue) query = query.Where(b => b.CityId == parameters.cityId.Value);
        if (parameters.isPublished.HasValue) query = query.Where(b => b.IsPublished == parameters.isPublished.Value);
        if (!string.IsNullOrWhiteSpace(parameters.languageCode)) query = query.Where(b => b.LanguageCode == parameters.languageCode);
        if (parameters.id is { Length: > 0 }) query = query.Where(b => parameters.id.Contains(b.Id));

        var totalCount = await query.CountAsync(ct);

        // 3. Сортировка
        var isDescending = string.Equals(parameters._order, "DESC", StringComparison.OrdinalIgnoreCase);
        query = parameters._sort?.ToLower() switch
        {
            "name" => isDescending ? query.OrderByDescending(b => b.Name) : query.OrderBy(b => b.Name),
            "slug" => isDescending ? query.OrderByDescending(b => b.Slug) : query.OrderBy(b => b.Slug),
            "provincename" => isDescending ? query.OrderByDescending(b => b.Province!.Name) : query.OrderBy(b => b.Province!.Name),
            "cityname" => isDescending ? query.OrderByDescending(b => b.City!.Name) : query.OrderBy(b => b.City!.Name),
            "updatedat" => isDescending ? query.OrderByDescending(b => b.UpdatedAt) : query.OrderBy(b => b.UpdatedAt),
            "createdat" => isDescending ? query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt),
            _ => query.OrderByDescending(b => b.CreatedAt)
        };

        // 4. Пагинация
        if (parameters._start.HasValue && parameters._end.HasValue)
        {
            var take = parameters._end.Value - parameters._start.Value;
            query = query.Skip(parameters._start.Value).Take(take > 0 ? take : 10);
        }

        var items = await query.ToListAsync(ct);
        return (items.Select(MapToDto), totalCount);
    }

    public async Task<BusinessPageResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var business = await db.BusinessPages
            .Include(b => b.Province).Include(b => b.City)
            .Include(b => b.PrimaryCategory).Include(b => b.SecondaryCategories)
            .Include(b => b.Tags).Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (business == null)
            logger.LogInformation("BusinessPage with ID {Id} not found.", id);

        return business is null ? null : MapToDto(business);
    }

    public async Task<(BusinessPageResponseDto? Result, Guid? ConflictingId, string? Error)> CreateAsync(
        BusinessPageUpsertDto dto,
        CancellationToken ct = default)
    {
        var slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug.ToLower().Trim();

        var existing = await db.BusinessPages.AsNoTracking()
            .Select(b => new { b.Id, b.Slug })
            .FirstOrDefaultAsync(b => b.Slug == slug, ct);

        if (existing != null)
        {
            logger.LogWarning("Create failed: Slug conflict for '{Slug}'. Conflicting ID: {Id}", slug, existing.Id);
            return (null, existing.Id, "Бизнес с таким URL-адресом уже существует.");
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
            Contacts = dto.Contacts ?? new BusinessContacts(),
            Schedule = dto.Schedule ?? [],
            Seo = dto.Seo ?? new BusinessSeoSettings(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await UpdateRelationsAsync(business, dto, ct);
        db.BusinessPages.Add(business);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(business.Id, ct), null, null);
    }

    public async Task<(BusinessPageResponseDto? Result, Guid? ConflictingId, string? Error)> UpdateAsync(
        Guid id,
        BusinessPageUpsertDto dto,
        CancellationToken ct = default)
    {
        var business = await db.BusinessPages
            .Include(b => b.SecondaryCategories).Include(b => b.Tags).Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (business == null)
        {
            logger.LogError("Update failed: BusinessPage {Id} not found.", id);
            return (null, null, "Страница не найдена.");
        }

        var newSlug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug.ToLower().Trim();

        if (business.Slug != newSlug)
        {
            var existing = await db.BusinessPages.AsNoTracking()
                .Where(b => b.Id != id)
                .Select(b => new { b.Id, b.Slug })
                .FirstOrDefaultAsync(b => b.Slug == newSlug, ct);

            if (existing != null)
            {
                logger.LogWarning("Update failed: Slug conflict for '{Slug}'. Conflicting ID: {Id}", newSlug, existing.Id);
                return (null, existing.Id, "Этот URL-адрес уже занят другим бизнесом.");
            }

            if (!business.OldSlugs.Contains(business.Slug)) business.OldSlugs.Add(business.Slug);
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
        business.Contacts = dto.Contacts ?? business.Contacts;
        business.Schedule = dto.Schedule ?? business.Schedule;
        business.Seo = dto.Seo ?? business.Seo;
        business.UpdatedAt = DateTimeOffset.UtcNow;

        await UpdateRelationsAsync(business, dto, ct);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(id, ct), null, null);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var business = await db.BusinessPages.FindAsync([id], ct);
        if (business == null) return false;

        db.BusinessPages.Remove(business);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task UpdateRelationsAsync(BusinessPage business, BusinessPageUpsertDto dto, CancellationToken ct)
    {
        business.SecondaryCategories.Clear();
        if (dto.SecondaryCategoryIds?.Count > 0)
        {
            var categories = await db.GoogleCategories.Where(c => dto.SecondaryCategoryIds.Contains(c.Id)).ToListAsync(ct);
            foreach (var cat in categories) business.SecondaryCategories.Add(cat);
        }

        business.Tags.Clear();
        if (dto.TagIds?.Count > 0)
        {
            var tags = await db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var tag in tags) business.Tags.Add(tag);
        }

        business.Media.Clear();
        if (dto.MediaIds?.Count > 0)
        {
            var assets = await db.MediaAssets.Where(m => dto.MediaIds.Contains(m.Id)).ToListAsync(ct);
            foreach (var asset in assets) business.Media.Add(asset);
        }
    }

    private static string GenerateSlug(string name) =>
        name.ToLowerInvariant().Replace(" ", "-").Where(c => char.IsLetterOrDigit(c) || c == '-').Aggregate("", (s, c) => s + c).Trim('-');

    private static BusinessPageResponseDto MapToDto(BusinessPage b) => new(
        b.Id, b.IsPublished, b.Name, b.Slug, b.OldSlugs, b.LanguageCode, b.Description,
        b.ProvinceId, b.Province?.Name, b.CityId, b.City?.Name,
        new GeoPointDto(b.Location.Y, b.Location.X),
        b.PrimaryCategoryId, b.PrimaryCategory?.NameEn,
        b.SecondaryCategories.Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs)),
        b.Contacts, b.Schedule, b.Seo,
        b.Tags.Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId, t.TagGroup?.NameEn)),
        b.Media.Select(m => new MediaAssetResponseDto(m.Id, m.Slug, m.FileName, m.ContentType, m.AltTextEn, m.AltTextEs, "", m.CreatedAt, [])),
        b.CreatedAt, b.UpdatedAt
    );
}