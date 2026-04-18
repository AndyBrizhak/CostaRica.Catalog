using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления бизнес-страницами.
/// Реализует паттерны для бесшовной интеграции с React Admin и обработки бизнес-конфликтов.
/// </summary>
public interface IBusinessPageService
{
    /// <summary>
    /// Получает список бизнес-страниц с поддержкой поиска, фильтрации и сортировки по связанным сущностям.
    /// </summary>
    /// <param name="parameters">Объект параметров запроса (_start, _end, _sort, _order, q и фильтры).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Кортеж: список DTO и общее количество записей для X-Total-Count.</returns>
    Task<(IEnumerable<BusinessPageResponseDto> Items, int TotalCount)> GetAllAsync(
        BusinessPageQueryParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Получает детальную информацию о бизнес-странице по её ID.
    /// </summary>
    Task<BusinessPageResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Создает новую бизнес-страницу.
    /// </summary>
    /// <returns>
    /// Result: созданный объект при успехе.
    /// ConflictingId: ID существующей записи, если возник конфликт (например, по Slug).
    /// Error: текст ошибки для отображения в уведомлении.
    /// </returns>
    Task<(BusinessPageResponseDto? Result, Guid? ConflictingId, string? Error)> CreateAsync(
        BusinessPageUpsertDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Обновляет данные существующей бизнес-страницы.
    /// Реализует логику сохранения истории слагов (OldSlugs).
    /// </summary>
    Task<(BusinessPageResponseDto? Result, Guid? ConflictingId, string? Error)> UpdateAsync(
        Guid id,
        BusinessPageUpsertDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Удаляет бизнес-страницу.
    /// </summary>
    /// <returns>True, если запись была найдена и удалена.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}