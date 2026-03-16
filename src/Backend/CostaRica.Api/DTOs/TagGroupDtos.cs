using System.ComponentModel.DataAnnotations;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Объект ответа для группы тегов (стандарт react-admin).
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
    [Required] string NameEn,
    [Required] string NameEs,
    [Required] string Slug);

/// <summary>
/// Параметры запроса для списка групп тегов (стандарт react-admin).
/// </summary>
public record TagGroupQueryParameters(
    int? _start = 0,
    int? _end = 10,
    string? _sort = "NameEn",
    string? _order = "ASC",
    string? NameEn = null,
    string? NameEs = null,
    string? Slug = null,
    string? Q = null // Глобальный поиск
);