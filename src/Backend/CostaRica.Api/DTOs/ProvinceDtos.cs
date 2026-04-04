using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

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
/// Параметры запроса для списка провинций.
/// Переведено в класс для обеспечения гибкого маппинга и ручного парсинга параметров react-admin.
/// </summary>
public class ProvinceQueryParameters
{
    [FromQuery(Name = "_start")]
    public int? Start { get; set; } = 0;

    [FromQuery(Name = "_end")]
    public int? End { get; set; } = 10;

    [FromQuery(Name = "_sort")]
    public string? Sort { get; set; } = "Name";

    [FromQuery(Name = "_order")]
    public string? Order { get; set; } = "ASC";

    [FromQuery(Name = "Q")]
    public string? Q { get; set; } = null;
}