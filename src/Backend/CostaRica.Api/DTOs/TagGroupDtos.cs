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
/// Параметры запроса для списка групп тегов.
/// Переведено в class для поддержки ручного парсинга JSON (filter, range, sort) в эндпоинтах.
/// </summary>
public class TagGroupQueryParameters
{
    // Стандартные параметры react-admin (пагинация и сортировка)
    public int? _start { get; set; } = 0;
    public int? _end { get; set; } = 9;
    public string? _sort { get; set; } = "NameEn";
    public string? _order { get; set; } = "ASC";

    // Точечные фильтры
    public string? NameEn { get; set; }
    public string? NameEs { get; set; }
    public string? Slug { get; set; }

    // Глобальный поиск (Q)
    public string? Q { get; set; }
}