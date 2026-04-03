using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface ICityService
{
    /// <summary>
    /// Получить список городов с поддержкой пагинации (_start/_end), 
    /// расширенной сортировки (_sort/_order) и фильтрации (включая глобальный поиск Q).
    /// </summary>
    /// <param name="parameters">Параметры запроса, совместимые с react-admin.</param>
    /// <returns>Кортеж: список DTO городов (с именем провинции) и общее количество записей.</returns>
    Task<(IEnumerable<CityResponseDto> Items, int TotalCount)> GetAllAsync(CityQueryParameters parameters);

    /// <summary>
    /// Получить данные конкретного города по его идентификатору.
    /// </summary>
    Task<CityResponseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Создать новый город с проверкой существования провинции и уникальности Slug.
    /// </summary>
    /// <param name="dto">Данные для создания.</param>
    /// <returns>DTO созданного города или null, если валидация не пройдена.</returns>
    Task<CityResponseDto?> CreateAsync(CityUpsertDto dto);

    /// <summary>
    /// Обновить данные существующего города.
    /// </summary>
    /// <param name="id">ID города.</param>
    /// <param name="dto">Новые данные.</param>
    /// <returns>True, если обновление успешно; иначе false.</returns>
    Task<bool> UpdateAsync(Guid id, CityUpsertDto dto);

    /// <summary>
    /// Удалить город из системы.
    /// </summary>
    /// <param name="id">ID города.</param>
    /// <returns>True, если город найден и удален.</returns>
    Task<bool> DeleteAsync(Guid id);
}