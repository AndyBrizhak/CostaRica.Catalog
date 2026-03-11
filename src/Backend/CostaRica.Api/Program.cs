using CostaRica.Api.Data;
using CostaRica.Api.Endpoints;
using CostaRica.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
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

// --- НОВОЕ: Регистрация системы медиа-ассетов ---

// 1. Регистрируем наше абстрактное хранилище
builder.Services.AddSingleton<IStorageService, LocalStorageProvider>();

// 2. Настраиваем ImageSharp для динамической обработки изображений
builder.Services.AddImageSharp()
    .Configure<PhysicalFileSystemProviderOptions>(options =>
    {
        // Указываем ImageSharp тот же путь, который мы пробросили из AppHost
        // Это позволит библиотеке находить файлы для ресайза и конвертации
        options.ProviderRootPath = builder.Configuration["Storage:LocalPath"] ?? "media";
    });

builder.Services.AddOpenApi();

var app = builder.Build();

// --- НОВОЕ: Middleware для обработки изображений ---
// Важно: UseImageSharp должен идти ДО эндпоинтов и обработки статических файлов
app.UseImageSharp();

// Сначала мапим служебные эндпоинты Aspire
app.MapDefaultEndpoints();

// Запуск миграций
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

// Мапинг существующих эндпоинтов
app.MapProvinceEndpoints();
app.MapCityEndpoints();
app.MapTagEndpoints();

// Примечание: app.MapMediaEndpoints() добавим в следующем шаге после создания файла.

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.Run();