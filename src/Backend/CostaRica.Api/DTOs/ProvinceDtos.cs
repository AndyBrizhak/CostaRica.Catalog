using System.ComponentModel.DataAnnotations;

namespace CostaRica.Api.DTOs;

/// <summary>
/// DTO для возврата данных о провинции (Read)
/// </summary>
public record ProvinceResponseDto(
    Guid Id,
    string Name,
    string Slug,
    IEnumerable<CityResponseDto>? Cities = null);

/// <summary>
/// DTO для создания или обновления провинции (Create/Update)
/// </summary>
public record ProvinceUpsertDto(
    [Required] string Name,
    [Required] string Slug);

/// <summary>
/// Параметры запроса для списка провинций (Золотой стандарт react-admin)
/// </summary>
public record ProvinceQueryParameters(
    int? _start = 0,
    int? _end = 10,
    string? _sort = "Name",
    string? _order = "ASC",
    string? Q = null // Глобальный поиск по Name или Slug
);