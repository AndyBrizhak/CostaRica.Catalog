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

    /// <summary>
    /// Получить конкретный город по ID
    /// </summary>
    Task<CityResponseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Создать город
    /// </summary>
    Task<CityResponseDto?> CreateAsync(CityUpsertDto dto);

    /// <summary>
    /// Обновить данные города.
    /// Согласно «Золотому стандарту», возвращает обновленный объект для синхронизации кэша фронтенда.
    /// </summary>
    /// <param name="id">Идентификатор города</param>
    /// <param name="dto">Данные для обновления</param>
    /// <returns>Обновленный CityResponseDto или null, если город не найден или возникла ошибка валидации</returns>
    Task<CityResponseDto?> UpdateAsync(Guid id, CityUpsertDto dto);

    /// <summary>
    /// Удалить город с проверкой на наличие зависимых сущностей (BusinessPages).
    /// </summary>
    /// <param name="id">Идентификатор города</param>
    /// <returns>Статус результата удаления (Success, NotFound, InUse)</returns>
    Task<CityDeleteResult> DeleteAsync(Guid id);
}