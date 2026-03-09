using CostaRica.Api.Data;
using CostaRica.Api.Endpoints;
using CostaRica.Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Инфраструктура Aspire
builder.AddServiceDefaults();

// 2. Настройка базы данных PostgreSQL (.NET 10 LTS)
builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    // Включаем поддержку NetTopologySuite для работы с географическими данными PostGIS
    options.UseNpgsql(o => o.UseNetTopologySuite());
});

// 3. Регистрация сервисов (Dependency Injection)
builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ITagGroupService, TagGroupService>();
builder.Services.AddScoped<ITagService, TagService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// 4. Улучшенная логика инициализации и миграции базы данных
// Мы разделяем "проверку связи" и "запуск миграций", чтобы избежать шума в логах
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<DirectoryDbContext>();

    const int maxRetries = 15;
    const int delayBetweenRetries = 2000;

    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Проверка готовности PostgreSQL (попытка {Attempt}/{Max})...", i, maxRetries);

            // Используем низкоуровневое соединение Npgsql, чтобы не провоцировать EF Core 
            // на выполнение запроса SELECT MigrationId до того, как база реально готова.
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            // Выполняем простейший запрос. Если он прошел — движок БД готов к работе.
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();
            await connection.CloseAsync();

            // Только после успешного SELECT 1 вызываем механизм миграций EF Core
            logger.LogInformation("База данных готова к приему команд. Применение миграций...");
            await db.Database.MigrateAsync();

            logger.LogInformation("База данных успешно инициализирована.");
            break;
        }
        catch (Exception)
        {
            if (i == maxRetries)
            {
                logger.LogCritical("Критическая ошибка: База данных не ответила после {Max} попыток.", maxRetries);
                throw;
            }

            logger.LogWarning("База данных еще прогревается. Повтор через {Delay}мс...", delayBetweenRetries);
            await Task.Delay(delayBetweenRetries);
        }
    }
}

app.MapDefaultEndpoints();

// 5. Подключение эндпоинтов всех модулей
app.MapProvinceEndpoints();
app.MapCityEndpoints();
app.MapTagEndpoints();

// 6. Настройки среды разработки
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();