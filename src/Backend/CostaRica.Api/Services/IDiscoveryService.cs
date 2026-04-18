using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface IDiscoveryService
{
    Task<(IEnumerable<BusinessPageCardDto> Items, int TotalCount)> SearchAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default);

    Task<IEnumerable<ProvinceResponseDto>> GetAvailableProvincesAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default);

    Task<IEnumerable<CityResponseDto>> GetAvailableCitiesAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default);

    Task<IEnumerable<TagResponseDto>> GetAvailableTagsAsync(
        DiscoverySearchParams @params,
        CancellationToken ct = default);

    Task<BusinessPageResponseDto?> GetBySlugAsync(
        string slug,
        CancellationToken ct = default);
}