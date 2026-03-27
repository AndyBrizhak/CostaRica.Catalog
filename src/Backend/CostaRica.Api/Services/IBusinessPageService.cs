using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

public interface IBusinessPageService
{
    Task<(IEnumerable<BusinessPageResponseDto> Items, int TotalCount)> GetAllAsync(BusinessPageQueryParameters parameters, CancellationToken ct = default);
    Task<BusinessPageResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Переход на кортежи (Result, ConflictingId, Error)
    Task<(BusinessPageResponseDto? Result, Guid? ConflictingId, string? Error)> CreateAsync(BusinessPageUpsertDto dto, CancellationToken ct = default);
    Task<(BusinessPageResponseDto? Result, Guid? ConflictingId, string? Error)> UpdateAsync(Guid id, BusinessPageUpsertDto dto, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}