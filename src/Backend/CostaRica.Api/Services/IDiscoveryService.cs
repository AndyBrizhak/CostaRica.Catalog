using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Сервис для публичного поиска и фильтрации (Discovery API).
/// Работает исключительно с опубликованными (IsPublished = true) записями.
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Основной поиск бизнесов с поддержкой гео-позиции, фильтров и пагинации.
    /// </summary>
    Task<(IEnumerable<BusinessPageCardDto> Items, int TotalCount)> SearchAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default);

    /// <summary>
    /// Получение списка доступных провинций с учетом текущих фильтров (теги, радиус).
    /// </summary>
    Task<IEnumerable<ProvinceResponseDto>> GetAvailableProvincesAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default);

    /// <summary>
    /// Получение списка доступных городов в выбранном регионе или радиусе.
    /// </summary>
    Task<IEnumerable<CityResponseDto>> GetAvailableCitiesAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default);

    /// <summary>
    /// Получение списка тегов, которые реально присутствуют у бизнесов, подходящих под текущие фильтры.
    /// </summary>
    Task<IEnumerable<TagResponseDto>> GetAvailableTagsAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default);

    /// <summary>
    /// Получение полной информации о бизнес-странице по её актуальному слагу.
    /// </summary>
    Task<BusinessPageResponseDto?> GetBySlugAsync(
        string slug,
        CancellationToken ct = default);
}