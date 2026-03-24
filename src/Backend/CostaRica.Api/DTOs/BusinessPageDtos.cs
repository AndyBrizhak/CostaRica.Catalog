using System.ComponentModel.DataAnnotations;
using CostaRica.Api.Data;

namespace CostaRica.Api.DTOs;

// --- ТИПЫ ДАННЫХ ДЛЯ ГЕОПОЗИЦИИ ---
public record GeoPointDto(double Latitude, double Longitude);

// --- ADMIN DTOs (Для React Admin) ---

public record BusinessPageResponseDto(
    Guid Id,
    bool IsPublished,
    string Name,
    string Slug,
    List<string> OldSlugs,
    string LanguageCode,
    string? Description,
    Guid ProvinceId,
    string? ProvinceName,
    Guid? CityId,
    string? CityName,
    GeoPointDto Location,
    Guid? PrimaryCategoryId,
    string? PrimaryCategoryName,
    IEnumerable<GoogleCategoryResponseDto> SecondaryCategories,
    BusinessContacts Contacts,
    List<ScheduleDay> Schedule,
    BusinessSeoSettings Seo,
    IEnumerable<TagResponseDto> Tags,
    IEnumerable<MediaAssetResponseDto> Media,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record BusinessPageUpsertDto(
    [Required] string Name,
    string? Slug,
    bool IsPublished,
    [Required] string LanguageCode,
    string? Description,
    [Required] Guid ProvinceId,
    Guid? CityId,
    Guid? PrimaryCategoryId,
    List<Guid> SecondaryCategoryIds,
    [Required] GeoPointDto Location,
    BusinessContacts Contacts,
    List<ScheduleDay> Schedule,
    BusinessSeoSettings Seo,
    List<Guid> TagIds,
    List<Guid> MediaIds
);

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

public record BusinessPageCardDto(
    string Name,
    string Slug,
    string? ThumbnailUrl,
    string? CityName,
    string? ProvinceName,
    string? PrimaryCategoryName,
    GeoPointDto Location,
    double? Distance
);

public record DiscoveryFiltersResponseDto(
    IEnumerable<ProvinceResponseDto> Provinces,
    IEnumerable<CityResponseDto> Cities,
    IEnumerable<TagResponseDto> Tags
);

/// <summary>
/// Параметры поиска. 
/// Использование массива Guid[] вместо List позволяет Minimal API корректно биндить параметры из Query String.
/// </summary>
public record DiscoverySearchParams(
    Guid? ProvinceId = null,
    Guid? CityId = null,
    double? Lat = null,
    double? Lon = null,
    double? RadiusInKm = null,
    Guid[]? TagIds = null, // Заменено на массив для корректного маппинга
    int Page = 1,
    int PageSize = 10
);