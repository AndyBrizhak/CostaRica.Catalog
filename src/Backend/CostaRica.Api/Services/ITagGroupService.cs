using CostaRica.Api.DTOs;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления группами тегов.
/// </summary>
public interface ITagGroupService
{
    /// <summary>
    /// Получить все группы тегов.
    /// </summary>
    Task<IEnumerable<TagGroupResponseDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить группу тегов по уникальному идентификатору.
    /// </summary>
    Task<TagGroupResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Получить группу тегов по её слагу (URL-friendly идентификатору).
    /// </summary>
    Task<TagGroupResponseDto?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Создать новую группу тегов.
    /// </summary>
    Task<TagGroupResponseDto> CreateAsync(TagGroupUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Обновить существующую группу тегов.
    /// </summary>
    Task<TagGroupResponseDto?> UpdateAsync(Guid id, TagGroupUpsertDto dto, CancellationToken ct = default);

    /// <summary>
    /// Удалить группу тегов.
    /// </summary>
    /// <returns>True, если удаление прошло успешно.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}