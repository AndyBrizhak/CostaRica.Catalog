namespace CostaRica.Api.DTOs;

/// <summary>
/// Результирующий DTO для отображения в списках и формах
/// </summary>
public record GoogleCategoryResponseDto(
    Guid Id,
    string Gcid,
    string NameEn,
    string NameEs);

/// <summary>
/// DTO для создания и обновления (Upsert)
/// </summary>
public record GoogleCategoryUpsertDto(
    string Gcid,
    string NameEn,
    string NameEs);