using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления медиа-ассетами и их метаданными.
/// Реализует принцип Exception-free.
/// </summary>
public interface IMediaAssetService
{
    /// <summary>
    /// Получает список ассетов на основе фильтров (по бизнесу, поиск, "сироты").
    /// </summary>
    Task<IEnumerable<MediaAssetResponseDto>> GetFilteredAsync(MediaFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// Получает один ассет по ID.
    /// </summary>
    Task<MediaAssetResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Загружает новый файл, сохраняет его в хранилище и создает запись в БД.
    /// </summary>
    /// <param name="fileStream">Поток данных файла.</param>
    /// <param name="originalFileName">Исходное имя файла (для определения расширения).</param>
    /// <param name="contentType">MIME-тип (image/jpeg и т.д.).</param>
    /// <param name="dto">Метаданные (SEO слаг, альты).</param>
    Task<MediaAssetResponseDto?> UploadAsync(Stream fileStream, string originalFileName, string contentType, MediaUploadDto dto, CancellationToken ct = default);

    /// <summary>
    /// Обновляет SEO-метаданные существующего ассета.
    /// </summary>
    Task<MediaAssetResponseDto?> UpdateMetadataAsync(Guid id, MediaUpdateDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удаляет ассет из базы и хранилища, если на него нет ссылок в бизнесах.
    /// </summary>
    Task<MediaDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Привязывает ассет к бизнес-странице.
    /// </summary>
    Task<bool> LinkToBusinessAsync(Guid assetId, Guid businessPageId, CancellationToken ct = default);

    /// <summary>
    /// Отвязывает ассет от бизнес-страницы.
    /// </summary>
    Task<bool> UnlinkFromBusinessAsync(Guid assetId, Guid businessPageId, CancellationToken ct = default);
}