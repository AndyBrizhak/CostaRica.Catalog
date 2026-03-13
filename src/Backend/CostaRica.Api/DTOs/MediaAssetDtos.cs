namespace CostaRica.Api.DTOs;

/// <summary>
/// Объект ответа для медиа-ассета.
/// </summary>
public record MediaAssetResponseDto(
    Guid Id,
    string Slug,
    string FileName,
    string ContentType,
    string? AltTextEn,
    string? AltTextEs,
    DateTimeOffset CreatedAt,
    // Список ID бизнесов, к которым привязан ассет (для инфо в админке)
    IEnumerable<Guid> RelatedBusinessIds);

/// <summary>
/// Объект для загрузки нового файла.
/// </summary>
public record MediaUploadDto(
    string Slug,
    string? AltTextEn,
    string? AltTextEs);

/// <summary>
/// Объект для обновления метаданных ассета (SEO).
/// </summary>
public record MediaUpdateDto(
    string Slug,
    string? AltTextEn,
    string? AltTextEs);

/// <summary>
/// Фильтр для поиска медиа-ассетов.
/// </summary>
public record MediaFilterDto(
    Guid? BusinessId = null,
    bool OnlyOrphans = false, // Только те, что не привязаны ни к одному бизнесу
    string? SearchTerm = null); // Поиск по слагу или альтам

/// <summary>
/// Результат операции удаления (для реализации Exception-free).
/// </summary>
public record MediaDeleteResult(
    bool Success,
    string? ErrorMessage = null);