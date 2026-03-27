using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CostaRica.Api.Services;

public class BusinessPageService(DirectoryDbContext db, ILogger<BusinessPageService> logger) : IBusinessPageService
{
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);
    private static readonly Guid DefaultCategoryId = Guid.Empty;

    public async Task<(IEnumerable<BusinessPageResponseDto> Items, int TotalCount)> GetAllAsync(BusinessPageQueryParameters parameters, CancellationToken ct = default)
    {
        var query = db.BusinessPages.AsNoTracking().Include(b => b.Province).Include(b => b.City).Include(b => b.PrimaryCategory).Include(b => b.SecondaryCategories).AsQueryable();

        if (parameters.provinceId.HasValue) query = query.Where(b => b.ProvinceId == parameters.provinceId.Value);
        if (parameters.cityId.HasValue) query = query.Where(b => b.CityId == parameters.cityId.Value);
        if (parameters.isPublished.HasValue) query = query.Where(b => b.IsPublished == parameters.isPublished.Value);

        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var search = $"%{parameters.q}%";
            query = query.Where(b => EF.Functions.ILike(b.Name, search) || (b.Description != null && EF.Functions.ILike(b.Description, search)));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(b => b.Name).Skip(parameters._start ?? 0).Take((parameters._end ?? 10) - (parameters._start ?? 0)).ToListAsync(ct);

        return (items.Select(MapToDto), totalCount);
    }

    public async Task<BusinessPageResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var business = await db.BusinessPages.AsNoTracking().Include(b => b.Province).Include(b => b.City).Include(b => b.PrimaryCategory).Include(b => b.SecondaryCategories).Include(b => b.Tags).Include(b => b.Media).FirstOrDefaultAsync(b => b.Id == id, ct);
        return business is not null ? MapToDto(business) : null;
    }

    public async Task<(BusinessPageResponseDto? Result, Guid? ConflictingId, string? Error)> CreateAsync(BusinessPageUpsertDto dto, CancellationToken ct = default)
    {
        var slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug;

        // Проверка конфликта слага
        var existing = await db.BusinessPages.AsNoTracking().Select(b => new { b.Id, b.Slug }).FirstOrDefaultAsync(b => b.Slug == slug, ct);
        if (existing != null)
            return (null, existing.Id, $"Бизнес со слагом '{slug}' уже существует.");

        var business = new BusinessPage
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = slug,
            IsPublished = dto.IsPublished,
            LanguageCode = dto.LanguageCode ?? "en",
            Description = dto.Description,
            ProvinceId = dto.ProvinceId,
            CityId = dto.CityId,
            PrimaryCategoryId = dto.PrimaryCategoryId ?? DefaultCategoryId,
            Location = _geometryFactory.CreatePoint(new Coordinate(dto.Location.Longitude, dto.Location.Latitude)),
            Contacts = dto.Contacts ?? new BusinessContacts(),
            Schedule = dto.Schedule ?? new List<ScheduleDay>(),
            Seo = dto.Seo ?? new BusinessSeoSettings(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        if (dto.SecondaryCategoryIds?.Count > 0)
            business.SecondaryCategories = await db.GoogleCategories.Where(c => dto.SecondaryCategoryIds.Contains(c.Id)).ToListAsync(ct);
        if (dto.TagIds?.Count > 0)
            business.Tags = await db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct);
        if (dto.MediaIds?.Count > 0)
            business.Media = await db.MediaAssets.Where(m => dto.MediaIds.Contains(m.Id)).ToListAsync(ct);

        try
        {
            db.BusinessPages.Add(business);
            await db.SaveChangesAsync(ct);
            return (await GetByIdAsync(business.Id, ct), null, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database error during CreateAsync");
            return (null, null, "Ошибка при сохранении в базу данных.");
        }
    }

    public async Task<(BusinessPageResponseDto? Result, Guid? ConflictingId, string? Error)> UpdateAsync(Guid id, BusinessPageUpsertDto dto, CancellationToken ct = default)
    {
        var business = await db.BusinessPages.Include(b => b.SecondaryCategories).Include(b => b.Tags).Include(b => b.Media).FirstOrDefaultAsync(b => b.Id == id, ct);
        if (business == null) return (null, null, "Страница не найдена.");

        if (!string.IsNullOrWhiteSpace(dto.Slug) && business.Slug != dto.Slug)
        {
            var conflict = await db.BusinessPages.AsNoTracking().Where(b => b.Slug == dto.Slug && b.Id != id).Select(b => new { b.Id }).FirstOrDefaultAsync(ct);
            if (conflict != null) return (null, conflict.Id, $"Слаг '{dto.Slug}' уже занят другим заведением.");

            if (!business.OldSlugs.Contains(business.Slug)) business.OldSlugs.Add(business.Slug);
            business.Slug = dto.Slug;
        }

        if (dto.Name != null) business.Name = dto.Name;
        business.IsPublished = dto.IsPublished;
        if (dto.LanguageCode != null) business.LanguageCode = dto.LanguageCode;
        if (dto.Description != null) business.Description = dto.Description;
        if (dto.ProvinceId != Guid.Empty) business.ProvinceId = dto.ProvinceId;
        if (dto.CityId.HasValue) business.CityId = dto.CityId;
        if (dto.PrimaryCategoryId.HasValue) business.PrimaryCategoryId = dto.PrimaryCategoryId;
        if (dto.Location != null) business.Location = _geometryFactory.CreatePoint(new Coordinate(dto.Location.Longitude, dto.Location.Latitude));
        if (dto.Contacts != null) business.Contacts = dto.Contacts;
        if (dto.Seo != null) business.Seo = dto.Seo;
        if (dto.Schedule != null) business.Schedule = dto.Schedule;

        if (dto.SecondaryCategoryIds != null)
        {
            business.SecondaryCategories.Clear();
            var cats = await db.GoogleCategories.Where(c => dto.SecondaryCategoryIds.Contains(c.Id)).ToListAsync(ct);
            foreach (var c in cats) business.SecondaryCategories.Add(c);
        }
        if (dto.TagIds != null)
        {
            business.Tags.Clear();
            var tags = await db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var t in tags) business.Tags.Add(t);
        }
        if (dto.MediaIds != null)
        {
            business.Media.Clear();
            var media = await db.MediaAssets.Where(m => dto.MediaIds.Contains(m.Id)).ToListAsync(ct);
            foreach (var m in media) business.Media.Add(m);
        }

        business.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
            return (await GetByIdAsync(business.Id, ct), null, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database error during UpdateAsync");
            return (null, null, "Ошибка при обновлении базы данных.");
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var business = await db.BusinessPages.FindAsync([id], ct);
        if (business == null) return false;
        db.BusinessPages.Remove(business);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateSlug(string name) => name.ToLowerInvariant().Replace(" ", "-").Where(c => char.IsLetterOrDigit(c) || c == '-').Aggregate("", (s, c) => s + c);

    private static BusinessPageResponseDto MapToDto(BusinessPage b) => new(
        b.Id, b.IsPublished, b.Name, b.Slug, b.OldSlugs ?? new List<string>(), b.LanguageCode, b.Description,
        b.ProvinceId, b.Province?.Name, b.CityId, b.City?.Name,
        new GeoPointDto(b.Location.Y, b.Location.X),
        b.PrimaryCategoryId, b.PrimaryCategory?.NameEn,
        b.SecondaryCategories.Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs)).ToList(),
        b.Contacts ?? new BusinessContacts(),
        b.Schedule ?? new List<ScheduleDay>(),
        b.Seo ?? new BusinessSeoSettings(),
        b.Tags.Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId)).ToList(),
        b.Media.Select(m => new MediaAssetResponseDto(m.Id, m.Slug, m.FileName, m.ContentType, m.AltTextEn, m.AltTextEs, $"/media/{m.Id}/{m.Slug}", m.CreatedAt, m.BusinessPages.Select(bp => bp.Id).ToList())).ToList(),
        b.CreatedAt, b.UpdatedAt
    );
}