using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface IGoogleCategoryService
{
    Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id);
    Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid);

    Task<(IEnumerable<GoogleCategoryResponseDto> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        int page = 1,
        int pageSize = 20,
        string? sortBy = "NameEn",
        bool isAscending = true);

    Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto);

    // МАССОВЫЙ ИМПОРТ
    Task<int> BulkImportAsync(IEnumerable<GoogleCategoryImportDto> categories);

    Task<bool> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto);
    Task<bool> DeleteAsync(Guid id);
}