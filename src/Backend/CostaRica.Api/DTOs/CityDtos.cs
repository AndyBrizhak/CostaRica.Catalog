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
/// Параметры запроса для списка городов.
/// Изменено на class с set для поддержки ручного парсинга JSON-фильтров.
/// </summary>
public class CityQueryParameters
{
    public int? _start { get; set; } = 0;
    public int? _end { get; set; } = 10;
    public string? _sort { get; set; } = "Name";
    public string? _order { get; set; } = "ASC";
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public Guid? ProvinceId { get; set; }
    public string? Q { get; set; } // Общий поиск
}