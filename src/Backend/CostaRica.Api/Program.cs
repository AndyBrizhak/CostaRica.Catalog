using CostaRica.Api.Data;
using CostaRica.Api.Endpoints;
using CostaRica.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    options.UseNpgsql(o => o.UseNetTopologySuite());
});

// Регистрация бизнес-сервисов
builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ITagGroupService, TagGroupService>();
builder.Services.AddScoped<ITagService, TagService>();

// --- СИСТЕМА МЕДИА-АССЕТОВ ---

// 1. Регистрируем наше абстрактное хранилище
builder.Services.AddSingleton<IStorageService, LocalStorageProvider>();

// Получаем базовый путь из конфигурации (проброшен из AppHost)
var storagePath = builder.Configuration["Storage:LocalPath"] ?? "media";

// 2. Настраиваем ImageSharp: указываем пути для оригиналов и для кэша
builder.Services.AddImageSharp()
    .Configure<PhysicalFileSystemProviderOptions>(options =>
    {
        options.ProviderRootPath = storagePath;
    })
    .Configure<PhysicalFileSystemCacheOptions>(options =>
    {
        // Явный путь к кэшу исправляет ошибку запуска в Aspire
        options.CacheRootPath = Path.Combine(storagePath, "is-cache");
    });

builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware для обработки изображений (должен быть до эндпоинтов)
app.UseImageSharp();

// Служебные эндпоинты Aspire
app.MapDefaultEndpoints();

// Запуск миграций при старте
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<DirectoryDbContext>();

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

// Мапинг эндпоинтов сущностей
app.MapProvinceEndpoints();
app.MapCityEndpoints();
app.MapTagEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();

    // ПЕРЕНАПРАВЛЕНИЕ: Чтобы при клике в дашборде Aspire открывался Scalar, а не 404
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.Run();