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
    Task<(IEnumerable<GoogleCategoryResponseDto> Items, int TotalCount)> GetAllAsync(
        GoogleCategoryQueryParameters parameters,
        CancellationToken ct = default);

    Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid, CancellationToken ct = default);

    Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Performs an atomic import of categories. 
    /// Validation: stops at the first conflict (GCID, NameEn, or NameEs) found in DB or input list.
    /// </summary>
    Task<BulkImportResponseDto> BulkImportAsync(IEnumerable<GoogleCategoryImportDto> categories, CancellationToken ct = default);

    Task<GoogleCategoryUpdateResult> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a category if no BusinessPages are using it.
    /// </summary>
    /// <returns>A tuple with the result and the number of linked BusinessPages.</returns>
    Task<(GoogleCategoryDeleteResult Result, int UsageCount)> DeleteAsync(Guid id, CancellationToken ct = default);
}