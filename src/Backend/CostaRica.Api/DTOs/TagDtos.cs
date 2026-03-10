namespace CostaRica.Api.DTOs;

/// <summary>
/// Объект ответа для тега.
/// </summary>
/// <param name="Id">Уникальный идентификатор тега.</param>
/// <param name="NameEn">Название на английском.</param>
/// <param name="NameEs">Название на испанском.</param>
/// <param name="Slug">Уникальный URL-слаг.</param>
/// <param name="TagGroupId">Идентификатор группы, к которой относится тег.</param>
public record TagResponseDto(
    Guid Id,
    string NameEn,
    string NameEs,
    string Slug,
    Guid TagGroupId);

/// <summary>
/// Объект для создания или обновления тега.
/// </summary>
/// <param name="NameEn">Название на английском.</param>
/// <param name="NameEs">Название на испанском.</param>
/// <param name="Slug">Уникальный URL-слаг.</param>
/// <param name="TagGroupId">ID родительской группы тегов.</param>
public record TagUpsertDto(
    string NameEn,
    string NameEs,
    string Slug,
    Guid TagGroupId);