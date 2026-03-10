using CostaRica.Api.Data;
using CostaRica.Api.Endpoints;
using CostaRica.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    options.UseNpgsql(o => o.UseNetTopologySuite());
});

builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ITagGroupService, TagGroupService>();
builder.Services.AddScoped<ITagService, TagService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// ПЕРЕНОСИМ СЮДА: Сначала мапим служебные эндпоинты (Health Checks), 
// чтобы Aspire видел, что приложение живое
app.MapDefaultEndpoints();

// Теперь запускаем миграции
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<DirectoryDbContext>();

    // В тестах база обычно поднимается быстрее, в Docker Desktop с PostGIS — дольше.
    // Используем CanConnectAsync, он легче и корректно работает через прокси Aspire.
    int maxRetries = 10;
    int delay = 2000;

    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            if (await db.Database.CanConnectAsync())
            {
                logger.LogInformation("Соединение с БД установлено. Применение миграций...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Миграции завершены.");
                break;
            }
        }
        catch (Exception ex)
        {
            if (i == maxRetries) throw;
            logger.LogWarning("БД пока не готова (попытка {i}). Ожидание...", i);
            await Task.Delay(delay);
        }
    }
}

app.MapProvinceEndpoints();
app.MapCityEndpoints();
app.MapTagEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Перенаправляем с корня (/) на интерфейс Scalar (/scalar/v1)
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}



app.Run();