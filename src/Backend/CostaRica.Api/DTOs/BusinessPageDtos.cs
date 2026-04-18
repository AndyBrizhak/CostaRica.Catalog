using System.ComponentModel.DataAnnotations;
using CostaRica.Api.Data;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Географические координаты (общие для всей системы).
/// </summary>
public record GeoPointDto(double Latitude, double Longitude);

/// <summary>
/// Полный объект ответа для административной панели.
/// Включает технические поля: IsPublished, OldSlugs, даты создания/обновления.
/// </summary>
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

/// <summary>
/// DTO для операций создания и обновления (Upsert).
/// </summary>
public record BusinessPageUpsertDto(
    [Required] string Name,
    string? Slug,
    bool IsPublished,
    [Required] string LanguageCode,
    string? Description,
    [Required] Guid ProvinceId,
    Guid? CityId,
    Guid? PrimaryCategoryId,
    List<Guid>? SecondaryCategoryIds,
    [Required] GeoPointDto Location,
    BusinessContacts Contacts,
    List<ScheduleDay> Schedule,
    BusinessSeoSettings Seo,
    List<Guid>? TagIds,
    List<Guid>? MediaIds
);

/// <summary>
/// Параметры запроса для React Admin.
/// Класс поддерживает ручной парсинг JSON из Query String.
/// </summary>
public class BusinessPageQueryParameters
{
    public int? _start { get; set; } = 0;
    public int? _end { get; set; } = 10;
    public string? _sort { get; set; } = "CreatedAt";
    public string? _order { get; set; } = "DESC";
    public string? q { get; set; }
    public Guid? provinceId { get; set; }
    public Guid? cityId { get; set; }
    public bool? isPublished { get; set; }
    public string? languageCode { get; set; }
    public Guid[]? id { get; set; }
}