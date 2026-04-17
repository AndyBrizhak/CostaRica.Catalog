using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

/// <summary>
/// Реализация сервиса управления медиа-ассетами.
/// </summary>
public class MediaAssetService(
    DirectoryDbContext db,
    IStorageService storage,
    ILogger<MediaAssetService> logger) : IMediaAssetService
{
    public async Task<(IEnumerable<MediaAssetResponseDto> Items, int TotalCount)> GetAllAsync(
        MediaQueryParameters parameters,
        CancellationToken ct = default)
    {
        var query = db.MediaAssets
            .AsNoTracking()
            .Include(m => m.BusinessPages)
            .AsQueryable();

        // 1. Фильтрация по списку ID
        if (parameters.Id != null && parameters.Id.Length > 0)
        {
            query = query.Where(m => parameters.Id.Contains(m.Id));
        }

        // 2. Фильтрация по конкретному бизнесу
        if (parameters.BusinessId.HasValue)
        {
            query = query.Where(m => m.BusinessPages.Any(b => b.Id == parameters.BusinessId.Value));
        }

        // 3. Фильтр "сирот" (Orphans)
        if (parameters.OnlyOrphans)
        {
            query = query.Where(m => !m.BusinessPages.Any());
        }

        // 4. Глобальный поиск
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            var searchTerm = parameters.Q.Trim().ToLower();
            query = query.Where(m =>
                m.Slug.ToLower().Contains(searchTerm) ||
                (m.AltTextEn != null && m.AltTextEn.ToLower().Contains(searchTerm)) ||
                (m.AltTextEs != null && m.AltTextEs.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        // 5. Сортировка с учетом регистра свойств C#
        // Сопоставляем camelCase из React Admin с PascalCase в Entity Framework
        var sortProperty = (parameters._sort?.ToLower()) switch
        {
            "createdat" => "CreatedAt",
            "slug" => "Slug",
            "filename" => "FileName",
            "contenttype" => "ContentType",
            _ => "CreatedAt" // По умолчанию сортируем по дате создания
        };

        query = parameters._order?.ToUpper() == "DESC"
            ? query.OrderByDescending(m => EF.Property<object>(m, sortProperty))
            : query.OrderBy(m => EF.Property<object>(m, sortProperty));

        // 6. Пагинация
        var items = await query
            .Skip(parameters._start ?? 0)
            .Take((parameters._end ?? 10) - (parameters._start ?? 0))
            .ToListAsync(ct);

        return (items.Select(MapToDto), totalCount);
    }

    public async Task<MediaAssetResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets
            .AsNoTracking()
            .Include(m => m.BusinessPages)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return asset != null ? MapToDto(asset) : null;
    }

    public async Task<MediaAssetResponseDto?> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        MediaUploadDto dto,
        CancellationToken ct = default)
    {
        if (await db.MediaAssets.AnyAsync(m => m.Slug == dto.Slug, ct))
        {
            logger.LogWarning("Попытка загрузки дубликата слага: {Slug}", dto.Slug);
            return null;
        }

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        var savedName = await storage.SaveAsync(fileStream, storedFileName, ct);
        if (savedName == null) return null;

        var asset = new MediaAsset
        {
            Slug = dto.Slug,
            FileName = savedName,
            ContentType = contentType,
            AltTextEn = dto.AltTextEn,
            AltTextEs = dto.AltTextEs
        };

        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync(ct);

        return MapToDto(asset);
    }

    public async Task<MediaAssetResponseDto?> UpdateMetadataAsync(Guid id, MediaUpdateDto dto, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets
            .Include(m => m.BusinessPages)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (asset == null) return null;

        asset.Slug = dto.Slug;
        asset.AltTextEn = dto.AltTextEn;
        asset.AltTextEs = dto.AltTextEs;

        await db.SaveChangesAsync(ct);
        return MapToDto(asset);
    }

    public async Task<MediaDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets
            .Include(m => m.BusinessPages)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (asset == null) return new MediaDeleteResult(MediaDeleteStatus.NotFound);

        int usageCount = asset.BusinessPages.Count;
        if (usageCount > 0)
        {
            return new MediaDeleteResult(MediaDeleteStatus.InUse, usageCount);
        }

        await storage.DeleteAsync(asset.FileName);
        db.MediaAssets.Remove(asset);
        await db.SaveChangesAsync(ct);

        return new MediaDeleteResult(MediaDeleteStatus.Success);
    }

    public async Task<bool> LinkToBusinessAsync(Guid assetId, Guid businessPageId, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets.Include(m => m.BusinessPages).FirstOrDefaultAsync(m => m.Id == assetId, ct);
        var business = await db.BusinessPages.FindAsync([businessPageId], ct);

        if (asset == null || business == null) return false;

        if (!asset.BusinessPages.Any(b => b.Id == businessPageId))
        {
            asset.BusinessPages.Add(business);
            await db.SaveChangesAsync(ct);
        }
        return true;
    }

    public async Task<bool> UnlinkFromBusinessAsync(Guid assetId, Guid businessPageId, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets.Include(m => m.BusinessPages).FirstOrDefaultAsync(m => m.Id == assetId, ct);
        if (asset == null) return false;

        var business = asset.BusinessPages.FirstOrDefault(b => b.Id == businessPageId);
        if (business != null)
        {
            asset.BusinessPages.Remove(business);
            await db.SaveChangesAsync(ct);
        }
        return true;
    }

    private MediaAssetResponseDto MapToDto(MediaAsset m) => new(
        m.Id,
        m.Slug,
        m.FileName,
        m.ContentType,
        m.AltTextEn,
        m.AltTextEs,
        storage.GetPublicUrl(m.FileName),
        m.CreatedAt,
        m.BusinessPages.Select(b => b.Id)
    );
}