using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс сервиса управления бизнес-страницами (Admin CRUD).
/// Предназначен для использования в административной панели.
/// </summary>
public interface IBusinessPageService
{
    /// <summary>
    /// Получение списка бизнес-страниц с фильтрацией, сортировкой и пагинацией.
    /// </summary>
    Task<(IEnumerable<BusinessPageResponseDto> Items, int TotalCount)> GetAllAsync(
        BusinessPageQueryParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Получение полной информации о бизнес-странице по ID.
    /// </summary>
    Task<BusinessPageResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Создание новой бизнес-страницы.
    /// </summary>
    Task<BusinessPageResponseDto?> CreateAsync(BusinessPageUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Обновление существующей бизнес-страницы.
    /// Реализует логику Dirty Check для отслеживания изменений Slug.
    /// </summary>
    Task<BusinessPageResponseDto?> UpdateAsync(Guid id, BusinessPageUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удаление бизнес-страницы и очистка связанных данных.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}