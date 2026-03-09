namespace CostaRica.Api.DTOs;

/// <summary>
/// DTO для возврата данных о городе
/// </summary>
public record CityResponseDto(
    Guid Id,
    string Name,
    string Slug,
    Guid ProvinceId);

/// <summary>
/// DTO для создания или обновления города
/// </summary>
public record CityUpsertDto(
    string Name,
    string Slug,
    Guid ProvinceId);