using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Результат операции удаления провинции
/// </summary>
public enum ProvinceDeleteResult
{
    Success,
    NotFound,
    InUse
}

/// <summary>
/// Интерфейс для управления бизнес-логикой провинций.
/// </summary>
public interface IProvinceService
{
    /// <summary>
    /// Получить список провинций с поддержкой поиска, фильтрации и пагинации (стандарт react-admin).
    /// </summary>
    /// <param name="params">Параметры запроса (Start, End, Sort, Order, Q).</param>
    /// <param name="includeCities">Флаг включения связанных городов.</param>
    /// <returns>Кортеж, содержащий список провинций и общее количество записей.</returns>
    Task<(IEnumerable<ProvinceResponseDto> Items, int TotalCount)> GetAllAsync(
        ProvinceQueryParameters @params,
        bool includeCities = false);

    Task<ProvinceResponseDto?> GetByIdAsync(Guid id, bool includeCities = false);

    Task<ProvinceResponseDto?> GetBySlugAsync(string slug, bool includeCities = false);

    Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto);

    /// <summary>
    /// Обновить данные провинции. 
    /// Возвращает обновленный объект (ProvinceResponseDto) для синхронизации кэша на фронтенде.
    /// </summary>
    Task<ProvinceResponseDto?> UpdateAsync(Guid id, ProvinceUpsertDto dto);

    /// <summary>
    /// Удалить провинцию. 
    /// Возвращает детализированный результат операции для предотвращения нарушения внешних ключей (города, бизнес-страницы).
    /// </summary>
    Task<ProvinceDeleteResult> DeleteAsync(Guid id);
}