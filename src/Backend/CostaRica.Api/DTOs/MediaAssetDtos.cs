using System.ComponentModel.DataAnnotations;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Статусы результата удаления медиа-ассета.
/// </summary>
public enum MediaDeleteStatus
{
    Success,
    NotFound,
    InUse
}

/// <summary>
/// Расширенный результат удаления с информацией о зависимостях.
/// </summary>
public record MediaDeleteResult(
    MediaDeleteStatus Status,
    int UsageCount = 0,
    string? Message = null);

/// <summary>
/// Параметры запроса для списка медиа-файлов.
/// Адаптировано под ручной парсинг параметров React Admin (sort, range, filter).
/// </summary>
public class MediaQueryParameters
{
    public int? _start { get; set; } = 0;
    public int? _end { get; set; } = 10;
    public string? _sort { get; set; } = "CreatedAt";
    public string? _order { get; set; } = "DESC";

    // Фильтры
    public Guid[]? Id { get; set; }
    public Guid? BusinessId { get; set; }
    public bool OnlyOrphans { get; set; }
    public string? Q { get; set; } // Глобальный поиск (Slug, AltText)
}

/// <summary>
/// Объект ответа для медиа-ассета (стандарт React Admin).
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
/// Объект для загрузки нового файла.
/// </summary>
public record MediaUploadDto(
    [Required] string Slug,
    string? AltTextEn,
    string? AltTextEs);

/// <summary>
/// Объект для обновления метаданных ассета.
/// </summary>
public record MediaUpdateDto(
    [Required] string Slug,
    string? AltTextEn,
    string? AltTextEs);