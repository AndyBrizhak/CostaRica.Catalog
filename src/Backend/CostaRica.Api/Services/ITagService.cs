using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления тегами (стандарт react-admin).
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Получить список тегов с поддержкой пагинации, фильтрации и поиска.
    /// Фильтрация по TagGroupId теперь интегрирована сюда.
    /// </summary>
    /// <param name="parameters">Параметры запроса (_start, _end, _sort, _order, TagGroupId, Q).</param>
    /// <returns>Кортеж: список DTO и общее количество записей для заголовка X-Total-Count.</returns>
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
    /// Создать новый тег. Возвращает null, если слаг занят или родительская группа не найдена.
    /// </summary>
    Task<TagResponseDto?> CreateAsync(TagUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Обновить данные тега. Возвращает null при конфликте слага или если тег/группа не найдены.
    /// </summary>
    Task<TagResponseDto?> UpdateAsync(Guid id, TagUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удалить тег.
    /// </summary>
    /// <returns>True, если удаление успешно.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}