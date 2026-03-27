using CostaRica.Api.Data;

namespace CostaRica.Api.Services;

/// <summary>
/// Интерфейс для управления аутентификацией и пользователями.
/// Следует подходу Exception-free, возвращая результат операции через AuthResult.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Авторизация пользователя и генерация JWT-токена.
    /// </summary>
    Task<AuthResult> LoginAsync(LoginRequest request);

    /// <summary>
    /// Регистрация нового пользователя с назначением роли.
    /// </summary>
    Task<AuthResult> RegisterAsync(RegisterRequest request, string role);

    /// <summary>
    /// Проверка существования пользователя (полезно для начальной инициализации системы).
    /// </summary>
    Task<bool> AnyUserExistsAsync();
}