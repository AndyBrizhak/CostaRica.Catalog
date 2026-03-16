using System.ComponentModel.DataAnnotations;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Объект ответа для тега (стандарт react-admin).
/// </summary>
public record TagResponseDto(
    Guid Id,
    string NameEn,
    string NameEs,
    string Slug,
    Guid TagGroupId);

/// <summary>
/// Объект для создания или обновления тега.
/// </summary>
public record TagUpsertDto(
    [Required] string NameEn,
    [Required] string NameEs,
    [Required] string Slug,
    [Required] Guid TagGroupId);

/// <summary>
/// Параметры запроса для списка тегов (стандарт react-admin).
/// </summary>
public record TagQueryParameters(
    int? _start = 0,
    int? _end = 10,
    string? _sort = "NameEn",
    string? _order = "ASC",
    string? NameEn = null,
    string? NameEs = null,
    string? Slug = null,
    Guid? TagGroupId = null, // Фильтр по родительской группе
    string? Q = null         // Глобальный поиск
);