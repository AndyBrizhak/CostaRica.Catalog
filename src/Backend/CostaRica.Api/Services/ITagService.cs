using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Возможные результаты удаления тега.
/// </summary>
public enum TagDeleteResult
{
    Success,
    NotFound,
    InUse
}

/// <summary>
/// Интерфейс для управления тегами (Золотой стандарт).
/// Обеспечивает интеграцию с React Admin и защиту целостности данных.
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Получить список тегов с поддержкой пагинации, фильтрации и глобального поиска.
    /// </summary>
    /// <param name="parameters">Параметры (_start, _end, _sort, _order, Q, TagGroupId).</param>
    /// <returns>Кортеж: список DTO и общее количество записей.</returns>
    Task<(IEnumerable<TagResponseDto> Items, int TotalCount)> GetAllAsync(TagQueryParameters parameters, CancellationToken ct = default);

    /// <summary>
    /// Получить тег по уникальному идентификатору.
    /// </summary>
    Task<TagResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Получить тег по его слагу.
    /// </summary>
    Task<TagResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Создать новый тег. 
    /// Возвращает null при конфликте слага или отсутствии группы.
    /// </summary>
    Task<TagResponseDto?> CreateAsync(TagUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Обновить данные тега. 
    /// Возвращает актуальный объект после сохранения.
    /// </summary>
    Task<TagResponseDto?> UpdateAsync(Guid id, TagUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удалить тег с проверкой зависимостей.
    /// </summary>
    /// <param name="id">ID тега.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус операции (Success, NotFound или InUse).</returns>
    Task<TagDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default);
}