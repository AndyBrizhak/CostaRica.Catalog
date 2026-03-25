namespace CostaRica.Api.Data;

/// <summary>
/// Запрос на вход в систему.
/// </summary>
public record LoginRequest(string Email, string Password);

/// <summary>
/// Запрос на регистрацию нового администратора/менеджера.
/// </summary>
public record RegisterRequest(string Email, string Password, string UserName);

/// <summary>
/// Универсальный результат операций аутентификации (Exception-free).
/// </summary>
public record AuthResult
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public IEnumerable<string>? Roles { get; init; }

    // Статические методы для удобного создания ответов без throw
    public static AuthResult Ok(string token, IEnumerable<string> roles) =>
        new() { Success = true, Token = token, Roles = roles };

    public static AuthResult Failure(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors };

    public static AuthResult Failure(string error) =>
        new() { Success = false, Errors = new[] { error } };
}