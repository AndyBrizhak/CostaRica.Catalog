using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

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

        // 1. Фильтрация по списку ID (запрос GET_MANY в React Admin)
        if (parameters.id != null && parameters.id.Length > 0)
        {
            query = query.Where(m => parameters.id.Contains(m.Id));
        }

        // 2. Фильтрация по привязке к конкретному бизнесу
        if (parameters.businessId.HasValue)
        {
            query = query.Where(m => m.BusinessPages.Any(b => b.Id == parameters.businessId.Value));
        }

        // 3. Фильтрация "сирот" (файлы без связей)
        if (parameters.onlyOrphans)
        {
            query = query.Where(m => m.BusinessPages.Count == 0);
        }

        // 4. Поиск (q) — используется ToLower().Contains() для совместимости с InMemory БД в тестах
        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var term = parameters.q.ToLower();
            query = query.Where(m =>
                m.Slug.ToLower().Contains(term) ||
                (m.AltTextEn != null && m.AltTextEn.ToLower().Contains(term)) ||
                (m.AltTextEs != null && m.AltTextEs.ToLower().Contains(term)) ||
                m.FileName.ToLower().Contains(term));
        }

        // 5. Подсчет общего количества до применения пагинации
        var totalCount = await query.CountAsync(ct);

        // 6. Динамическая сортировка
        query = parameters._sort switch
        {
            "Slug" => parameters._order == "DESC" ? query.OrderByDescending(m => m.Slug) : query.OrderBy(m => m.Slug),
            "ContentType" => parameters._order == "DESC" ? query.OrderByDescending(m => m.ContentType) : query.OrderBy(m => m.ContentType),
            "CreatedAt" => parameters._order == "DESC" ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt),
            _ => query.OrderByDescending(m => m.CreatedAt) // По умолчанию — новые сверху
        };

        // 7. Пагинация на основе границ _start и _end
        var start = parameters._start ?? 0;
        var end = parameters._end ?? 10;
        var take = Math.Max(end - start, 0);

        var items = await query.Skip(start).Take(take).ToListAsync(ct);

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
        string originalFileName,
        string contentType,
        MediaUploadDto dto,
        CancellationToken ct = default)
    {
        // Проверка уникальности слага
        var slugExists = await db.MediaAssets.AnyAsync(m => m.Slug == dto.Slug, ct);
        if (slugExists) return null;

        // Сохранение физического файла
        var fileName = await storage.SaveAsync(fileStream, originalFileName, ct);
        if (fileName == null) return null;

        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            Slug = dto.Slug,
            FileName = fileName,
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
        var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (asset == null) return null;

        // Если слаг меняется, проверяем его уникальность среди других записей
        if (asset.Slug != dto.Slug)
        {
            var exists = await db.MediaAssets.AnyAsync(m => m.Slug == dto.Slug && m.Id != id, ct);
            if (exists) return null;
        }

        asset.Slug = dto.Slug;
        asset.AltTextEn = dto.AltTextEn;
        asset.AltTextEs = dto.AltTextEs;

        await db.SaveChangesAsync(ct);
        return MapToDto(asset);
    }

    public async Task<MediaDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var asset = await db.MediaAssets
                .Include(m => m.BusinessPages)
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (asset == null) return new MediaDeleteResult(false, "Ассет не найден");

            // Защита от удаления: нельзя удалять то, что используется на страницах
            if (asset.BusinessPages.Count > 0)
                return new MediaDeleteResult(false, $"Невозможно удалить: ассет используется в {asset.BusinessPages.Count} бизнес-страницах");

            var fileName = asset.FileName;

            // Сначала удаляем из БД
            db.MediaAssets.Remove(asset);
            await db.SaveChangesAsync(ct);

            // Только при успехе в БД удаляем файл физически
            await storage.DeleteAsync(fileName);

            return new MediaDeleteResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении медиа-ассета {Id}", id);
            return new MediaDeleteResult(false, "Внутренняя ошибка сервера при удалении");
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
        $"/media/{m.Id}/{m.Slug}", // Формируем SEO-friendly URL
        m.CreatedAt,
        m.BusinessPages.Select(b => b.Id)
    );
}