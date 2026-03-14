using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface IGoogleCategoryService
{
    Task<IEnumerable<GoogleCategoryResponseDto>> GetAllAsync();
    Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id);
    Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid);

    //  Поиск с пагинацией ---
    // searchTerm - строка для поиска по NameEn или NameEs
    Task<(IEnumerable<GoogleCategoryResponseDto> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        int page = 1,
        int pageSize = 20);

    Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto);
    Task<bool> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto);
    Task<bool> DeleteAsync(Guid id);
}