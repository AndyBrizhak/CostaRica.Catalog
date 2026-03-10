namespace CostaRica.Api.DTOs;

/// <summary>
/// Объект ответа для группы тегов.
/// </summary>
/// <param name="Id">Уникальный идентификатор группы.</param>
/// <param name="NameEn">Название на английском.</param>
/// <param name="NameEs">Название на испанском.</param>
/// <param name="Slug">Уникальный URL-слаг.</param>
public record TagGroupResponseDto(
    Guid Id,
    string NameEn,
    string NameEs,
    string Slug);

/// <summary>
/// Объект для создания или обновления группы тегов.
/// </summary>
public record TagGroupUpsertDto(
    string NameEn,
    string NameEs,
    string Slug);