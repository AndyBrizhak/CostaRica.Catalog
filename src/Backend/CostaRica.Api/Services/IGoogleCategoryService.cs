using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface IGoogleCategoryService
{
    Task<IEnumerable<GoogleCategoryResponseDto>> GetAllAsync();
    Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id);
    Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid);

    // Создание (вернет null, если Gcid уже существует)
    Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto);

    Task<bool> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto);
    Task<bool> DeleteAsync(Guid id);
}