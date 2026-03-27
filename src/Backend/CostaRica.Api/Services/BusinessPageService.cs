using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CostaRica.Api.Services;

public class BusinessPageService(DirectoryDbContext db, ILogger<BusinessPageService> logger) : IBusinessPageService
{
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);
    private static readonly Guid DefaultCategoryId = Guid.Empty; // 00000000-0000-0000-0000-000000000000

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

        // Базовая логика уникальности слага для первого шага
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
            LanguageCode = dto.LanguageCode ?? "en",
            Description = dto.Description,
            ProvinceId = dto.ProvinceId,
            CityId = dto.CityId,

            // Если категория не указана - подставляем дефолтный Guid.Empty
            PrimaryCategoryId = dto.PrimaryCategoryId ?? DefaultCategoryId,

            Location = _geometryFactory.CreatePoint(new Coordinate(dto.Location.Longitude, dto.Location.Latitude)),

            // ЗАЩИТА: Инициализация объектов, чтобы избежать NULL в Postgres JSONB
            Contacts = dto.Contacts ?? new BusinessContacts(),
            Schedule = dto.Schedule ?? new List<ScheduleDay>(),
            Seo = dto.Seo ?? new BusinessSeoSettings(),

            OldSlugs = new List<string>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Привязка коллекций
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

        // 1. SEO Менеджмент: управление историей слагов
        if (!string.IsNullOrWhiteSpace(dto.Slug) && business.Slug != dto.Slug)
        {
            // Проверяем, нет ли уже такого слага у других (базовая проверка)
            var isSlugTaken = await db.BusinessPages.AnyAsync(b => b.Slug == dto.Slug && b.Id != id, ct);
            if (!isSlugTaken)
            {
                // Сохраняем старый слаг в историю, если его там еще нет
                if (!business.OldSlugs.Contains(business.Slug))
                    business.OldSlugs.Add(business.Slug);

                business.Slug = dto.Slug;
            }
        }

        // 2. Базовые поля: обновляем только если передано не null значение
        if (dto.Name != null) business.Name = dto.Name;
        if (dto.LanguageCode != null) business.LanguageCode = dto.LanguageCode;
        if (dto.Description != null) business.Description = dto.Description;
        business.IsPublished = dto.IsPublished;

        // 3. Связи (Foreign Keys)
        if (dto.ProvinceId != Guid.Empty) business.ProvinceId = dto.ProvinceId;
        if (dto.CityId.HasValue) business.CityId = dto.CityId;
        if (dto.PrimaryCategoryId.HasValue) business.PrimaryCategoryId = dto.PrimaryCategoryId;

        // 4. Локация (PostGIS)
        if (dto.Location != null)
        {
            business.Location = _geometryFactory.CreatePoint(new Coordinate(dto.Location.Longitude, dto.Location.Latitude));
        }

        // 5. JSONB Сложные типы (Owned Types)
        // Если в DTO null — оставляем как было. Если пришел объект — заменяем.
        if (dto.Contacts != null) business.Contacts = dto.Contacts;
        if (dto.Seo != null) business.Seo = dto.Seo;
        if (dto.Schedule != null) business.Schedule = dto.Schedule;

        // 6. Синхронизация коллекций (Many-to-Many)
        // Обновляем только если список ID был явно передан (не null)
        if (dto.SecondaryCategoryIds != null)
        {
            business.SecondaryCategories.Clear();
            var cats = await db.GoogleCategories.Where(c => dto.SecondaryCategoryIds.Contains(c.Id)).ToListAsync(ct);
            foreach (var cat in cats) business.SecondaryCategories.Add(cat);
        }

        if (dto.TagIds != null)
        {
            business.Tags.Clear();
            var tags = await db.Tags.Where(t => dto.TagIds.Contains(t.Id)).ToListAsync(ct);
            foreach (var tag in tags) business.Tags.Add(tag);
        }

        if (dto.MediaIds != null)
        {
            business.Media.Clear();
            var media = await db.MediaAssets.Where(m => dto.MediaIds.Contains(m.Id)).ToListAsync(ct);
            foreach (var m in media) business.Media.Add(m);
        }

        business.UpdatedAt = DateTimeOffset.UtcNow;

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
        b.Id,                                   // 1
        b.IsPublished,                          // 2
        b.Name,                                 // 3
        b.Slug,                                 // 4
        b.OldSlugs ?? new List<string>(),       // 5
        b.LanguageCode,                         // 6
        b.Description,                          // 7
        b.ProvinceId,                           // 8
        b.Province?.Name,                       // 9
        b.CityId,                               // 10
        b.City?.Name,                           // 11
        new GeoPointDto(b.Location.Y, b.Location.X), // 12
        b.PrimaryCategoryId,                    // 13
        b.PrimaryCategory?.NameEn,              // 14
        b.SecondaryCategories.Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs)).ToList(), // 15
        b.Contacts ?? new BusinessContacts(),   // 16
        b.Schedule ?? new List<ScheduleDay>(),  // 17
        b.Seo ?? new BusinessSeoSettings(),     // 18
        b.Tags.Select(t => new TagResponseDto(t.Id, t.NameEn, t.NameEs, t.Slug, t.TagGroupId)).ToList(), // 19
        b.Media.Select(m => new MediaAssetResponseDto( // 20
            m.Id, m.Slug, m.FileName, m.ContentType, m.AltTextEn, m.AltTextEs,
            $"/media/{m.Id}/{m.Slug}", m.CreatedAt, m.BusinessPages.Select(bp => bp.Id).ToList()
        )).ToList(),
        b.CreatedAt,                            // 21
        b.UpdatedAt                             // 22
    );
}