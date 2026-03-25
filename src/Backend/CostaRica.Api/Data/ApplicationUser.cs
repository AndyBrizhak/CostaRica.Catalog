using Microsoft.AspNetCore.Identity;

namespace CostaRica.Api.Data;

/// <summary>
/// Расширенная модель пользователя. 
/// Использует Guid в качестве первичного ключа для единообразия с остальными сущностями проекта.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    // На данном этапе модель остается пустой, так как мы используем 
    // только стандартные поля IdentityUser (Email, UserName, PasswordHash и т.д.).
}