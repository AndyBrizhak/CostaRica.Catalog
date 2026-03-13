using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class MediaAssetService(
    DirectoryDbContext db,
    IStorageService storage,
    ILogger<MediaAssetService> logger) : IMediaAssetService
{
    public async Task<IEnumerable<MediaAssetResponseDto>> GetFilteredAsync(MediaFilterDto filter, CancellationToken ct = default)
    {
        var query = db.MediaAssets
            .AsNoTracking()
            .Include(m => m.BusinessPages)
            .AsQueryable();

        // Фильтр: Только те, что привязаны к конкретному бизнесу
        if (filter.BusinessId.HasValue)
        {
            query = query.Where(m => m.BusinessPages.Any(b => b.Id == filter.BusinessId.Value));
        }

        // Фильтр: "Сироты" (не привязаны ни к одному бизнесу)
        if (filter.OnlyOrphans)
        {
            query = query.Where(m => m.BusinessPages.Count == 0);
        }

        // Поиск по слагу или альтам
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(m => m.Slug.ToLower().Contains(term)
                                || (m.AltTextEn != null && m.AltTextEn.ToLower().Contains(term))
                                || (m.AltTextEs != null && m.AltTextEs.ToLower().Contains(term)));
        }

        var assets = await query.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
        return assets.Select(MapToDto);
    }

    public async Task<MediaAssetResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets
            .AsNoTracking()
            .Include(m => m.BusinessPages)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return asset == null ? null : MapToDto(asset);
    }

    public async Task<MediaAssetResponseDto?> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        MediaUploadDto dto,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Проверка уникальности слага
            if (await db.MediaAssets.AnyAsync(m => m.Slug == dto.Slug, ct))
            {
                logger.LogWarning("Загрузка отменена: слаг {Slug} уже занят", dto.Slug);
                return null;
            }

            // 2. Генерируем имя файла: ID + оригинальное расширение
            var extension = Path.GetExtension(originalFileName);
            var assetId = Guid.NewGuid();
            var fileName = $"{assetId}{extension}";

            // 3. Сохраняем физический файл через IStorageService
            var savedName = await storage.SaveAsync(fileStream, fileName, ct);
            if (savedName == null) return null;

            // 4. Создаем запись в БД
            var asset = new MediaAsset
            {
                Id = assetId,
                Slug = dto.Slug,
                FileName = savedName,
                ContentType = contentType,
                AltTextEn = dto.AltTextEn,
                AltTextEs = dto.AltTextEs,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(ct);

            return MapToDto(asset);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Критическая ошибка при загрузке медиа-ассета");
            return null;
        }
    }

    public async Task<MediaAssetResponseDto?> UpdateMetadataAsync(Guid id, MediaUpdateDto dto, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets.Include(m => m.BusinessPages).FirstOrDefaultAsync(m => m.Id == id, ct);
        if (asset == null) return null;

        // Проверка уникальности нового слага
        if (asset.Slug != dto.Slug && await db.MediaAssets.AnyAsync(m => m.Slug == dto.Slug, ct))
            return null;

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

            // ТРЕБОВАНИЕ: Запрет удаления, если есть привязки к бизнесам
            if (asset.BusinessPages.Any())
            {
                return new MediaDeleteResult(false, $"Невозможно удалить: ассет используется в {asset.BusinessPages.Count} бизнес-страницах");
            }

            // 1. Удаляем физический файл
            var fileDeleted = await storage.DeleteAsync(asset.FileName);
            if (!fileDeleted) logger.LogWarning("Файл {File} не был найден на диске при удалении ассета", asset.FileName);

            // 2. Удаляем запись из БД
            db.MediaAssets.Remove(asset);
            await db.SaveChangesAsync(ct);

            return new MediaDeleteResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении ассета {Id}", id);
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
        m.CreatedAt,
        m.BusinessPages.Select(b => b.Id));
}