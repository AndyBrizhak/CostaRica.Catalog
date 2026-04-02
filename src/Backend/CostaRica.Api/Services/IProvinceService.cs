using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления бизнес-логикой провинций.
/// </summary>
public interface IProvinceService
{
    /// <summary>
    /// Получить список провинций с поддержкой поиска, фильтрации и пагинации (стандарт react-admin).
    /// </summary>
    /// <param name="params">Параметры запроса (_start, _end, _sort, _order, Q).</param>
    /// <param name="includeCities">Флаг включения связанных городов.</param>
    /// <returns>Кортеж, содержащий список провинций и общее количество записей.</returns>
    Task<(IEnumerable<ProvinceResponseDto> Items, int TotalCount)> GetAllAsync(
        ProvinceQueryParameters @params,
        bool includeCities = false);

    Task<ProvinceResponseDto?> GetByIdAsync(Guid id, bool includeCities = false);

    Task<ProvinceResponseDto?> GetBySlugAsync(string slug, bool includeCities = false);

    Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto);

    Task<bool> UpdateAsync(Guid id, ProvinceUpsertDto dto);

    Task<bool> DeleteAsync(Guid id);
}