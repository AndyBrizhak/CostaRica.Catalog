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
    string Name,
    string Slug);