using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Results for updating a Google Category.
/// </summary>
public enum GoogleCategoryUpdateResult
{
    Success,
    NotFound,
    Conflict // Used when GCID is already taken by another category
}

/// <summary>
/// Results for deleting a Google Category.
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

    Task<int> BulkImportAsync(IEnumerable<GoogleCategoryImportDto> categories, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing category with duplicate GCID check.
    /// </summary>
    Task<GoogleCategoryUpdateResult> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a category if it's not referenced by any BusinessPages.
    /// </summary>
    /// <returns>A tuple containing the result and the count of dependent BusinessPages.</returns>
    Task<(GoogleCategoryDeleteResult Result, int UsageCount)> DeleteAsync(Guid id, CancellationToken ct = default);
}