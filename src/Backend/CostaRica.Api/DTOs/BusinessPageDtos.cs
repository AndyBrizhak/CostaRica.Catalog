using System.ComponentModel.DataAnnotations;
using CostaRica.Api.Data;

namespace CostaRica.Api.DTOs;

// --- ТИПЫ ДАННЫХ ДЛЯ ГЕОПОЗИЦИИ ---
public record GeoPointDto(double Latitude, double Longitude);

// --- ADMIN DTOs (Для React Admin) ---

/// <summary>
/// Полный DTO бизнес-страницы для админ-панели.
/// </summary>
public record BusinessPageResponseDto(
    Guid Id,
    bool IsPublished,
    string Name,
    string Slug,
    string LanguageCode,
    string? Description,
    Guid ProvinceId,
    string? ProvinceName,
    Guid? CityId,
    string? CityName,
    GeoPointDto Location,
    Guid? PrimaryCategoryId,
    string? PrimaryCategoryName,
    BusinessContacts Contacts,
    List<ScheduleDay> Schedule,
    BusinessSeoSettings Seo,
    IEnumerable<TagResponseDto> Tags,
    IEnumerable<MediaAssetResponseDto> Media,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

/// <summary>
/// DTO для создания и обновления бизнес-страницы.
/// </summary>
public record BusinessPageUpsertDto(
    [Required] string Name,
    string? Slug, // Если пустой, сервис сгенерирует автоматически
    bool IsPublished,
    string LanguageCode,
    string? Description,
    [Required] Guid ProvinceId,
    Guid? CityId,
    [Required] GeoPointDto Location,
    Guid? PrimaryCategoryId,
    List<Guid> SecondaryCategoryIds,
    BusinessContacts Contacts,
    List<ScheduleDay> Schedule,
    BusinessSeoSettings Seo,
    List<Guid> TagIds,
    List<Guid> MediaIds
);

/// <summary>
/// Параметры запроса списка для React Admin.
/// </summary>
public record BusinessPageQueryParameters(
    int? _start = 0,
    int? _end = 10,
    string? _sort = "Name",
    string? _order = "ASC",
    string? q = null,
    Guid? provinceId = null,
    Guid? cityId = null,
    bool? isPublished = null
);

// --- DISCOVERY DTOs (Для публичного фронтенда) ---

/// <summary>
/// Легкий DTO для карточки бизнеса в результатах поиска.
/// </summary>
public record BusinessPageCardDto(
    string Name,
    string Slug,
    string? ThumbnailUrl,
    string? CityName,
    string? ProvinceName,
    string? PrimaryCategoryName,
    GeoPointDto Location,
    double? Distance // Заполняется только при гео-поиске
);

/// <summary>
/// Ответ со списками доступных фильтров (Smart Availability).
/// </summary>
public record DiscoveryFiltersResponseDto(
    IEnumerable<ProvinceResponseDto> Provinces,
    IEnumerable<CityResponseDto> Cities,
    IEnumerable<TagResponseDto> Tags
);

/// <summary>
/// Универсальные параметры для публичного поиска.
/// </summary>
public record DiscoverySearchParams(
    Guid? ProvinceId = null,
    Guid? CityId = null,
    double? Lat = null,
    double? Lon = null,
    double? RadiusInKm = null,
    List<Guid>? TagIds = null,
    int Page = 1,
    int PageSize = 20
);