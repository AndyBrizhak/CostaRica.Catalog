using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления медиа-ассетами.
/// Обеспечивает логику хранения файлов и управления их SEO-метаданными.
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
    /// Загружает новый файл в хранилище и создает запись в БД.
    /// </summary>
    Task<MediaAssetResponseDto?> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        MediaUploadDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Обновляет SEO-метаданные (Slug, Alt-тексты).
    /// </summary>
    Task<MediaAssetResponseDto?> UpdateMetadataAsync(Guid id, MediaUpdateDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удаляет ассет из базы и хранилища с обязательной проверкой зависимостей.
    /// </summary>
    /// <param name="id">ID ассета.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат со статусом и счетчиком использования в бизнес-страницах.</returns>
    Task<MediaDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Устанавливает связь между ассетом и бизнес-страницей.
    /// </summary>
    Task<bool> LinkToBusinessAsync(Guid assetId, Guid businessPageId, CancellationToken ct = default);

    /// <summary>
    /// Разрывает связь между ассетом и бизнес-страницей.
    /// </summary>
    Task<bool> UnlinkFromBusinessAsync(Guid assetId, Guid businessPageId, CancellationToken ct = default);
}