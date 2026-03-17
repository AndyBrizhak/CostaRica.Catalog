using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления бизнес-логикой провинций.
/// </summary>
public interface IProvinceService
{
    /// <summary>
    /// Получить список провинций с поддержкой поиска, фильтрации и пагинации.
    /// </summary>
    /// <param name="searchTerm">Строка поиска (по имени или слагу).</param>
    /// <param name="page">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Количество записей на странице.</param>
    /// <param name="sortBy">Поле для сортировки.</param>
    /// <param name="isAscending">Направление сортировки.</param>
    /// <param name="includeCities">Флаг включения связанных городов.</param>
    /// <returns>Кортеж, содержащий список провинций и общее количество записей, подходящих под условия поиска.</returns>
    Task<(IEnumerable<ProvinceResponseDto> Items, int TotalCount)> GetAllAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool isAscending = true,
        bool includeCities = false);

    Task<ProvinceResponseDto?> GetByIdAsync(Guid id, bool includeCities = false);

    Task<ProvinceResponseDto?> GetBySlugAsync(string slug, bool includeCities = false);

    Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto);

    Task<bool> UpdateAsync(Guid id, ProvinceUpsertDto dto);

    Task<bool> DeleteAsync(Guid id);
}