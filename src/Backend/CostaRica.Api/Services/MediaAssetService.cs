using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

/// <summary>
/// Реализация сервиса управления медиа-ассетами.
/// Использует Primary Constructor (C# 12) для внедрения зависимостей.
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

        // 1. Фильтрация по списку ID (запрос GET_MANY)
        if (parameters.Id != null && parameters.Id.Length > 0)
        {
            query = query.Where(m => parameters.Id.Contains(m.Id));
        }

        // 2. Фильтрация по конкретному бизнесу
        if (parameters.BusinessId.HasValue)
        {
            query = query.Where(m => m.BusinessPages.Any(b => b.Id == parameters.BusinessId.Value));
        }

        // 3. ФИЛЬТР СИРОТ (Orphans): только те, у кого нет связей со страницами
        if (parameters.OnlyOrphans)
        {
            query = query.Where(m => !m.BusinessPages.Any());
        }

        // 4. ГЛОБАЛЬНЫЙ ПОИСК (Q) через ILike (PostgreSQL)
        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            var search = $"%{parameters.Q}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.Slug, search) ||
                EF.Functions.ILike(m.AltTextEn ?? "", search) ||
                EF.Functions.ILike(m.AltTextEs ?? "", search) ||
                EF.Functions.ILike(m.FileName, search));
        }

        var totalCount = await query.CountAsync(ct);

        // 5. ДИНАМИЧЕСКАЯ СОРТИРОВКА
        var isDescending = string.Equals(parameters._order, "DESC", StringComparison.OrdinalIgnoreCase);
        query = parameters._sort?.ToLower() switch
        {
            "slug" => isDescending ? query.OrderByDescending(m => m.Slug) : query.OrderBy(m => m.Slug),
            "filename" => isDescending ? query.OrderByDescending(m => m.FileName) : query.OrderBy(m => m.FileName),
            "createdat" => isDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt),
            _ => query.OrderByDescending(m => m.CreatedAt) // По умолчанию: новые сверху
        };

        // 6. ПАГИНАЦИЯ
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 10;
        var take = end - start;

        var items = await query
            .Skip(start)
            .Take(take > 0 ? take : 10)
            .Select(m => MapToDto(m))
            .ToListAsync(ct);

        return (items, totalCount);
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
        string originalFileName,
        string contentType,
        MediaUploadDto dto,
        CancellationToken ct = default)
    {
        var fileExtension = Path.GetExtension(originalFileName);
        var internalFileName = $"{Guid.NewGuid()}{fileExtension}";

        var savedPath = await storage.SaveAsync(fileStream, internalFileName, ct);
        if (savedPath == null) return null;

        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            Slug = dto.Slug.ToLowerInvariant(),
            FileName = internalFileName,
            ContentType = contentType,
            AltTextEn = dto.AltTextEn,
            AltTextEs = dto.AltTextEs,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync(ct);

        return MapToDto(asset);
    }

    public async Task<MediaAssetResponseDto?> UpdateMetadataAsync(Guid id, MediaUpdateDto dto, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets.FindAsync([id], ct);
        if (asset == null) return null;

        asset.Slug = dto.Slug.ToLowerInvariant();
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

        if (asset == null)
            return new MediaDeleteResult(MediaDeleteStatus.NotFound);

        // ПРОВЕРКА ЗАВИСИМОСТЕЙ: считаем количество страниц, использующих это фото
        int usageCount = asset.BusinessPages.Count;
        if (usageCount > 0)
        {
            logger.LogWarning("Попытка удаления используемого ассета {Id}. Страниц: {Count}", id, usageCount);
            return new MediaDeleteResult(
                MediaDeleteStatus.InUse,
                usageCount,
                $"Cannot delete asset: it is used by {usageCount} business page(s).");
        }

        try
        {
            // Сначала удаляем физический файл
            await storage.DeleteAsync(asset.FileName);

            // Затем запись в БД
            db.MediaAssets.Remove(asset);
            await db.SaveChangesAsync(ct);

            return new MediaDeleteResult(MediaDeleteStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении ассета {Id}", id);
            throw;
        }
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

    private static MediaAssetResponseDto MapToDto(MediaAsset m) => new(
        m.Id,
        m.Slug,
        m.FileName,
        m.ContentType,
        m.AltTextEn,
        m.AltTextEs,
        $"/media-files/{m.FileName}", // Путь для фронтенда
        m.CreatedAt,
        m.BusinessPages.Select(b => b.Id));
}