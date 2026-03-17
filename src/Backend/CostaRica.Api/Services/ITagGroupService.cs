using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления группами тегов (стандарт react-admin).
/// </summary>
public interface ITagGroupService
{
    /// <summary>
    /// Получить список групп тегов с поддержкой пагинации, фильтрации и поиска.
    /// </summary>
    /// <param name="parameters">Параметры запроса (_start, _end, _sort, _order, Q).</param>
    /// <returns>Кортеж: список DTO и общее количество записей для заголовка X-Total-Count.</returns>
    Task<(IEnumerable<TagGroupResponseDto> Items, int TotalCount)> GetAllAsync(TagGroupQueryParameters parameters, CancellationToken ct = default);

    /// <summary>
    /// Получить группу тегов по ID.
    /// </summary>
    Task<TagGroupResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Получить группу тегов по слагу.
    /// </summary>
    Task<TagGroupResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Создать новую группу тегов. Возвращает null, если слаг уже занят.
    /// </summary>
    Task<TagGroupResponseDto?> CreateAsync(TagGroupUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Обновить группу тегов. Возвращает null, если группа не найдена или новый слаг занят.
    /// </summary>
    Task<TagGroupResponseDto?> UpdateAsync(Guid id, TagGroupUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удалить группу тегов.
    /// </summary>
    /// <returns>True, если удаление успешно.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}