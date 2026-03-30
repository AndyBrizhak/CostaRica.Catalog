using System.Security.Claims;
using CostaRica.Api.Data;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для административного управления пользователями.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Получает постраничный список пользователей с учетом фильтрации и сортировки.
    /// </summary>
    /// <param name="range">Строка JSON с параметрами пагинации (например, [0,9]).</param>
    /// <param name="sort">Строка JSON с параметрами сортировки (например, ["email","ASC"]).</param>
    /// <returns>Кортеж, содержащий список пользователей и общее количество записей в БД.</returns>
    Task<(IEnumerable<object> Users, int TotalCount)> GetPagedUsersAsync(string? range, string? sort);

    /// <summary>
    /// Получает детальную информацию о пользователе и его ролях.
    /// </summary>
    Task<object?> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Удаляет пользователя с проверкой иерархии ролей.
    /// </summary>
    /// <param name="id">ID пользователя для удаления.</param>
    /// <param name="actor">Текущий пользователь (из ClaimsPrincipal), инициировавший удаление.</param>
    /// <returns>Результат операции IdentityResult.</returns>
    Task<ServiceResult> DeleteUserAsync(Guid id, ClaimsPrincipal actor);
}

/// <summary>
/// Простой вспомогательный класс для возврата результатов из сервиса.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; } = 200;

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Failure(string message, int statusCode = 400) =>
        new() { Succeeded = false, ErrorMessage = message, StatusCode = statusCode };
}