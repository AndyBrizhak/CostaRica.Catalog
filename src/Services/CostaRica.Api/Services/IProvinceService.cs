using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления бизнес-логикой провинций.
/// </summary>
public interface IProvinceService
{
    /// <summary>
    /// Получить список всех провинций.
    /// </summary>
    Task<IEnumerable<ProvinceResponseDto>> GetAllAsync();

    /// <summary>
    /// Получить провинцию по идентификатору.
    /// </summary>
    Task<ProvinceResponseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Создать новую провинцию.
    /// </summary>
    Task<ProvinceResponseDto> CreateAsync(ProvinceUpsertDto dto);

    /// <summary>
    /// Обновить данные существующей провинции.
    /// </summary>
    /// <returns>True, если обновление успешно; False, если провинция не найдена.</returns>
    Task<bool> UpdateAsync(Guid id, ProvinceUpsertDto dto);

    /// <summary>
    /// Удалить провинцию.
    /// </summary>
    /// <returns>True, если удаление успешно; False, если провинция не найдена.</returns>
    Task<bool> DeleteAsync(Guid id);
}