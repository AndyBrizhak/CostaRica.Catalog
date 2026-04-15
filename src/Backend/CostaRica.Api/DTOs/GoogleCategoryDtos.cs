using Microsoft.AspNetCore.Mvc;

namespace CostaRica.Api.DTOs;

/// <summary>
/// Объект ответа для категории Google (стандарт react-admin).
/// </summary>
public record GoogleCategoryResponseDto(
    Guid Id,
    string Gcid,
    string NameEn,
    string NameEs);

/// <summary>
/// Объект для создания или обновления категории.
/// </summary>
public record GoogleCategoryUpsertDto(
    string Gcid,
    string NameEn,
    string NameEs);

/// <summary>
/// Объект для импорта категорий из JSON.
/// </summary>
public record GoogleCategoryImportDto(
    string Gcid,
    string NameEn,
    string NameEs);

/// <summary>
/// Результат массового импорта.
/// </summary>
public record BulkImportResponseDto(
    int ImportedCount,
    bool HasConflict,
    string? ErrorMessage = null,
    string? ConflictType = null);

/// <summary>
/// Параметры запроса для списка категорий.
/// Очищено от логики парсинга для ручного наполнения в эндпоинтах (как в TagQueryParameters).
/// </summary>
public class GoogleCategoryQueryParameters
{
    public int? _start { get; set; } = 0;
    public int? _end { get; set; } = 10;
    public string? _sort { get; set; } = "NameEn";
    public string? _order { get; set; } = "ASC";

    // Фильтры
    public string? Q { get; set; } // Глобальный поиск
    public Guid[]? id { get; set; } // Для запросов GET_MANY
}