using Microsoft.AspNetCore.Identity;

namespace CostaRica.Api.Data;

public static class DataSeeder
{
    public static async Task SeedIdentityAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Создание ролей, если их нет
        string[] roleNames = { "SuperAdmin", "Admin", "Manager", "Viewer" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
            }
        }

        // 2. Создание первого SuperAdmin, если база пользователей пуста
        var adminSettings = configuration.GetSection("InitialAdmin");
        var adminEmail = adminSettings["Email"] ?? "admin@costarica.local";
        var adminPassword = adminSettings["Password"] ?? "Admin123!";

        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser == null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "superadmin",
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createAdminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }
    }
}