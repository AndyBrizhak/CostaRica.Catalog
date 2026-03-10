using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления тегами.
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Получить все доступные теги.
    /// </summary>
    Task<IEnumerable<TagResponseDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить список тегов, принадлежащих конкретной группе.
    /// </summary>
    Task<IEnumerable<TagResponseDto>> GetByGroupIdAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>
    /// Получить тег по уникальному идентификатору.
    /// </summary>
    Task<TagResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Получить тег по его слагу.
    /// </summary>
    Task<TagResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Создать новый тег.
    /// </summary>
    Task<TagResponseDto> CreateAsync(TagUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Обновить данные существующего тега.
    /// </summary>
    Task<TagResponseDto?> UpdateAsync(Guid id, TagUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удалить тег.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}