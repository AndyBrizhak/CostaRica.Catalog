using System.ComponentModel.DataAnnotations;

namespace CostaRica.Api.DTOs;

/// <summary>
/// DTO для возврата данных о городе (адаптировано под react-admin)
/// </summary>
public record CityResponseDto(
    Guid Id,
    string Name,
    string Slug,
    Guid ProvinceId,
    string? ProvinceName = null);

/// <summary>
/// DTO для создания или обновления города
/// </summary>
public record CityUpsertDto(
    [Required] string Name,
    [Required] string Slug,
    [Required] Guid ProvinceId);

/// <summary>
/// Параметры запроса для списка городов (стандарт react-admin)
/// </summary>
public record CityQueryParameters(
    int? _start = 0,
    int? _end = 10,
    string? _sort = "Name",
    string? _order = "ASC",
    string? Name = null,
    string? Slug = null,
    Guid? ProvinceId = null,
    string? Q = null // Общий поиск
);