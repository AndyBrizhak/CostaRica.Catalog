using CostaRica.Api.Data;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Параметры поиска. Используем record для поддержки оператора 'with' в сервисе.
/// </summary>
public record DiscoverySearchParams(
    string? Q = null,
    Guid? ProvinceId = null,
    Guid? CityId = null,
    Guid? CategoryId = null,
    IEnumerable<Guid>? TagIds = null,
    double? Lat = null,
    double? Lon = null,
    double? RadiusInKm = null,
    int Page = 1,
    int PageSize = 12
);

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