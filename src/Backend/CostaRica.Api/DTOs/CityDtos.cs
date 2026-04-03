using System.ComponentModel.DataAnnotations;

namespace CostaRica.Api.DTOs;

public record CityResponseDto(
    Guid Id,
    string Name,
    string Slug,
    Guid ProvinceId,
    string? ProvinceName = null);

public record CityUpsertDto(
    [Required] string Name,
    [Required] string Slug,
    [Required] Guid ProvinceId);

/// <summary>
/// Параметры запроса. Изменено на class для поддержки ручного парсинга JSON.
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
    public string? Q { get; set; }
}