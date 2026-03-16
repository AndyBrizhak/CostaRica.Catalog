using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface IGoogleCategoryService
{
    Task<(IEnumerable<GoogleCategoryResponseDto> Items, int TotalCount)> GetAllAsync(
        GoogleCategoryQueryParameters parameters,
        CancellationToken ct = default);

    Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid, CancellationToken ct = default);

    Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto, CancellationToken ct = default);
    Task<int> BulkImportAsync(IEnumerable<GoogleCategoryImportDto> categories, CancellationToken ct = default);

    Task<bool> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}