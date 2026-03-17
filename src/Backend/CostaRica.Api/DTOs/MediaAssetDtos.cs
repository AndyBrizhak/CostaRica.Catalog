namespace CostaRica.Api.DTOs;

/// <summary>
/// Параметры запроса для списка медиа-файлов (стандарт React Admin)
/// </summary>
public record MediaQueryParameters(
    int? _start = 0,
    int? _end = 10,
    string? _sort = "CreatedAt",
    string? _order = "DESC",
    string? q = null,
    Guid[]? id = null,
    Guid? businessId = null,
    bool onlyOrphans = false
);

/// <summary>
/// Объект ответа для медиа-ассета (плоская структура)
/// </summary>
public record MediaAssetResponseDto(
    Guid Id,
    string Slug,
    string FileName,
    string ContentType,
    string? AltTextEn,
    string? AltTextEs,
    string Url,
    DateTimeOffset CreatedAt,
    IEnumerable<Guid> RelatedBusinessIds);

/// <summary>
/// Объект для загрузки нового файла
/// </summary>
public record MediaUploadDto(
    string Slug,
    string? AltTextEn,
    string? AltTextEs);

/// <summary>
/// Объект для обновления метаданных ассета
/// </summary>
public record MediaUpdateDto(
    string Slug,
    string? AltTextEn,
    string? AltTextEs);

/// <summary>
/// Результат операции удаления (Exception-free)
/// </summary>
public record MediaDeleteResult(bool Success, string? ErrorMessage = null);