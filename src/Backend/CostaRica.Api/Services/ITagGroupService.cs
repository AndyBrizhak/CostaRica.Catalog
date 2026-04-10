using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления группами тегов (стандарт react-admin).
/// </summary>
public interface ITagGroupService
{
    /// <summary>
    /// Получить список групп тегов с поддержкой пагинации, фильтрации и глобального поиска.
    /// </summary>
    /// <param name="parameters">Параметры запроса (_start, _end, _sort, _order, Q).</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Кортеж: список DTO и общее количество записей для заголовка X-Total-Count.</returns>
    Task<(IEnumerable<TagGroupResponseDto> Items, int TotalCount)> GetAllAsync(TagGroupQueryParameters parameters, CancellationToken ct = default);

    /// <summary>
    /// Получить группу тегов по уникальному идентификатору.
    /// </summary>
    Task<TagGroupResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Получить группу тегов по её слагу (SEO).
    /// </summary>
    Task<TagGroupResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Создать новую группу тегов. 
    /// Согласно стандарту, возвращает созданный объект (через GetById) для синхронизации кэша фронтенда.
    /// </summary>
    Task<TagGroupResponseDto?> CreateAsync(TagGroupUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Обновить данные группы тегов.
    /// Возвращает обновленный объект или null, если группа не найдена или возник конфликт данных.
    /// </summary>
    Task<TagGroupResponseDto?> UpdateAsync(Guid id, TagGroupUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удалить группу тегов. 
    /// Возвращает результат операции: Success, NotFound или InUse.
    /// </summary>
    Task<TagGroupDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default);
}