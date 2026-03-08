using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface IProvinceService
{
    // Получить все провинции. Параметр includeCities позволяет подгрузить города.
    Task<IEnumerable<ProvinceResponseDto>> GetAllAsync(bool includeCities = false);

    // Получить провинцию по ID.
    Task<ProvinceResponseDto?> GetByIdAsync(Guid id, bool includeCities = false);

    // Получить провинцию по ее Slug (необходимо для публичных страниц каталога).
    Task<ProvinceResponseDto?> GetBySlugAsync(string slug, bool includeCities = false);

    // Создать провинцию (возвращает null, если Slug уже занят)
    Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto);

    // Обновить данные провинции
    Task<bool> UpdateAsync(Guid id, ProvinceUpsertDto dto);

    // Удалить провинцию
    Task<bool> DeleteAsync(Guid id);
}