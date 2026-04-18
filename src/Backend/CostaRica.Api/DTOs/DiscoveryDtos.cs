using CostaRica.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Параметры поиска в публичном каталоге.
/// Используем class и [FromQuery] для устранения ошибок инференса в Minimal API.
/// </summary>
public class DiscoverySearchParams
{
    [FromQuery(Name = "q")] public string? Q { get; set; }
    [FromQuery(Name = "provinceId")] public Guid? ProvinceId { get; set; }
    [FromQuery(Name = "cityId")] public Guid? CityId { get; set; }
    [FromQuery(Name = "categoryId")] public Guid? CategoryId { get; set; }

    // Использование массива позволяет корректно принимать список ID из Query String
    [FromQuery(Name = "tagIds")] public Guid[]? TagIds { get; set; }

    [FromQuery(Name = "lat")] public double? Lat { get; set; }
    [FromQuery(Name = "lon")] public double? Lon { get; set; }
    [FromQuery(Name = "radiusInKm")] public double? RadiusInKm { get; set; }
    [FromQuery(Name = "page")] public int Page { get; set; } = 1;
    [FromQuery(Name = "pageSize")] public int PageSize { get; set; } = 12;
}

public record BusinessPageCardDto(
    Guid Id,
    string Name,
    string Slug,
    string? ThumbnailUrl,
    string? CityName,
    string? ProvinceName,
    string? PrimaryCategoryName,
    GeoPointDto Location,
    double? Distance
);

public record BusinessPageDiscoveryDto(
    Guid Id,
    string Name,
    string Slug,
    string LanguageCode,
    string? Description,
    string? ProvinceName,
    string? CityName,
    GeoPointDto Location,
    string? PrimaryCategoryName,
    IEnumerable<GoogleCategoryResponseDto> SecondaryCategories,
    BusinessContacts Contacts,
    List<ScheduleDay> Schedule,
    BusinessSeoSettings Seo,
    IEnumerable<TagResponseDto> Tags,
    IEnumerable<MediaAssetResponseDto> Media
);

public record DiscoveryFiltersResponseDto(
    IEnumerable<ProvinceResponseDto> Provinces,
    IEnumerable<CityResponseDto> Cities,
    IEnumerable<TagResponseDto> Tags,
    IEnumerable<GoogleCategoryResponseDto> Categories
);