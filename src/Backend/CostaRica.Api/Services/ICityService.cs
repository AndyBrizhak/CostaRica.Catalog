using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface ICityService
{
    /// <summary>
    /// Получить список городов с поддержкой пагинации, фильтрации и сортировки (стандарт react-admin)
    /// </summary>
    /// <param name="parameters">Параметры запроса (_start, _end, _sort, _order, фильтры)</param>
    /// <returns>Кортеж: список DTO городов и общее количество записей</returns>
    Task<(IEnumerable<CityResponseDto> Items, int TotalCount)> GetAllAsync(CityQueryParameters parameters);

    // Получить конкретный город по ID
    Task<CityResponseDto?> GetByIdAsync(Guid id);

    // Создать город
    Task<CityResponseDto?> CreateAsync(CityUpsertDto dto);

    // Обновить данные города
    Task<bool> UpdateAsync(Guid id, CityUpsertDto dto);

    // Удалить город
    Task<bool> DeleteAsync(Guid id);
}