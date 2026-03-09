using CostaRica.Api.Data;
using CostaRica.Api.Endpoints;
using CostaRica.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Инфраструктура Aspire
builder.AddServiceDefaults();

// 2. Настройка базы данных PostgreSQL
builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    // Используем NetTopologySuite для работы с PostGIS (необходим для BusinessPage.Location)
    options.UseNpgsql(o => o.UseNetTopologySuite());
});

// 3. РЕГИСТРАЦИЯ СЕРВИСОВ (Dependency Injection)
builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<ICityService, CityService>();

// 4. Генерация OpenAPI документации
builder.Services.AddOpenApi();

var app = builder.Build();

// 5. Автоматическое применение миграций при старте (Защита от холодного старта)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<DirectoryDbContext>();

    const int maxRetries = 5;
    const int delayMilliseconds = 3000; // 3 секунды между попытками

    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Попытка применения миграций {Attempt}/{MaxRetries}...", i, maxRetries);
            await db.Database.MigrateAsync();
            logger.LogInformation("Миграции успешно применены.");
            break;
        }
        catch (Exception ex)
        {
            if (i == maxRetries)
            {
                logger.LogCritical(ex, "Ошибка: база данных не ответила после {MaxRetries} попыток.", maxRetries);
                throw;
            }

            logger.LogWarning("База данных еще не готова (Попытка {Attempt}). Ожидание {Delay}ms...", i, delayMilliseconds);
            await Task.Delay(delayMilliseconds);
        }
    }
}

app.MapDefaultEndpoints();

// 6. Настройки среды разработки
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

// 7. РЕГИСТРАЦИЯ ЭНДПОИНТОВ
app.MapProvinceEndpoints();
app.MapCityEndpoints();

app.Run();