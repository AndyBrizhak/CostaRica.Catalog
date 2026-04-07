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
    Guid TagGroupId,
    string? TagGroupName = null); // Добавлено для отображения и сортировки по имени группы

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
/// Изменено на class для ручного наполнения данными при десериализации JSON в эндпоинтах.
/// </summary>
public class TagQueryParameters
{
    public int? _start { get; set; } = 0;
    public int? _end { get; set; } = 10;
    public string? _sort { get; set; } = "NameEn";
    public string? _order { get; set; } = "ASC";

    // Фильтры
    public Guid? TagGroupId { get; set; }
    public string? Q { get; set; } // Глобальный поиск
}