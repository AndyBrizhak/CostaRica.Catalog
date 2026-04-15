using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Possible results for an update operation.
/// </summary>
public enum GoogleCategoryUpdateResult
{
    Success,
    NotFound,
    Conflict // Used when GCID or Names are already taken
}

/// <summary>
/// Possible results for a delete operation.
/// </summary>
public enum GoogleCategoryDeleteResult
{
    Success,
    NotFound,
    InUse // Used when category is linked to BusinessPages
}

public interface IGoogleCategoryService
{
    /// <summary>
    /// Retrieves a paginated and filtered list of categories.
    /// Matches the 'Smart Search' and 'Smart Sort' pattern.
    /// </summary>
    Task<(IEnumerable<GoogleCategoryResponseDto> Items, int TotalCount)> GetAllAsync(
        GoogleCategoryQueryParameters parameters,
        CancellationToken ct = default);

    Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Finds a category by its string identifier (e.g., "restaurant").
    /// </summary>
    Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid, CancellationToken ct = default);

    Task<GoogleCategoryResponseDto> CreateAsync(GoogleCategoryUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Performs an atomic import of categories from a list.
    /// </summary>
    Task<BulkImportResponseDto> BulkImportAsync(List<GoogleCategoryImportDto> categories, CancellationToken ct = default);

    Task<GoogleCategoryUpdateResult> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a category if it's not currently in use by any Business Pages.
    /// </summary>
    Task<(GoogleCategoryDeleteResult Result, int UsageCount)> DeleteAsync(Guid id, CancellationToken ct = default);
}