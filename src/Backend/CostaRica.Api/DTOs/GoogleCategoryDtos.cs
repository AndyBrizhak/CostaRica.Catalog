namespace CostaRica.Api.DTOs;

/// <summary>
/// DTO для возврата категории Google
/// </summary>
public record GoogleCategoryResponseDto(
    Guid Id,
    string Gcid,
    string NameEn,
    string NameEs);

/// <summary>
/// DTO для создания или обновления категории
/// </summary>
public record GoogleCategoryUpsertDto(
    string Gcid,
    string NameEn,
    string NameEs);