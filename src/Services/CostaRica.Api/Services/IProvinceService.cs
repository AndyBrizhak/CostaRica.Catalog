using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface IProvinceService
{
    Task<IEnumerable<ProvinceResponseDto>> GetAllAsync();
    Task<ProvinceResponseDto?> GetByIdAsync(Guid id);

    // Возвращает null, если произошел конфликт (например, дубликат Slug)
    Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto);

    Task<bool> UpdateAsync(Guid id, ProvinceUpsertDto dto);
    Task<bool> DeleteAsync(Guid id);
}