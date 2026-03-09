using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface ICityService
{
    // Получить все города
    Task<IEnumerable<CityResponseDto>> GetAllAsync();

    // Получить конкретный город по ID
    Task<CityResponseDto?> GetByIdAsync(Guid id);

    // Получить список городов конкретной провинции по её слагу (SEO-Friendly)
    Task<IEnumerable<CityResponseDto>> GetByProvinceAsync(string provinceSlug);

    // Создать город (возвращает null, если произошел конфликт или провинция не найдена)
    Task<CityResponseDto?> CreateAsync(CityUpsertDto dto);

    // Обновить данные города
    Task<bool> UpdateAsync(Guid id, CityUpsertDto dto);

    // Удалить город
    Task<bool> DeleteAsync(Guid id);
}