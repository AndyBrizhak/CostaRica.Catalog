using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления медиа-ассетами.
/// </summary>
public interface IMediaAssetService
{
    /// <summary>
    /// Получает список ассетов с пагинацией, фильтрацией и поиском.
    /// </summary>
    Task<(IEnumerable<MediaAssetResponseDto> Items, int TotalCount)> GetAllAsync(
        MediaQueryParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Получает один ассет по ID.
    /// </summary>
    Task<MediaAssetResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Загружает новый файл.
    /// </summary>
    Task<MediaAssetResponseDto?> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        MediaUploadDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Обновляет SEO-метаданные.
    /// </summary>
    Task<MediaAssetResponseDto?> UpdateMetadataAsync(Guid id, MediaUpdateDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удаляет ассет из базы и хранилища (безопасно).
    /// </summary>
    Task<MediaDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Привязывает ассет к бизнесу.
    /// </summary>
    Task<bool> LinkToBusinessAsync(Guid assetId, Guid businessPageId, CancellationToken ct = default);

    /// <summary>
    /// Отвязывает ассет от бизнеса.
    /// </summary>
    Task<bool> UnlinkFromBusinessAsync(Guid assetId, Guid businessPageId, CancellationToken ct = default);
}