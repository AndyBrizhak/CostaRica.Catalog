using CostaRica.Api.Data;
using CostaRica.Api.Endpoints;
using CostaRica.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.DependencyInjection;
using SixLabors.ImageSharp.Web.Providers;

// ==================================================================================
// [CRITICAL: DO NOT REMOVE] Блок совместимости для миграций EF Core и PostgreSQL.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisablePostgres80StrictTypeChecking", true);
// ==================================================================================

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    options.UseNpgsql(o =>
    {
        o.UseNetTopologySuite();
        o.EnableRetryOnFailure();
    });
});

// Регистрация бизнес-сервисов
builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ITagGroupService, TagGroupService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IGoogleCategoryService, GoogleCategoryService>();

// --- НОВОЕ: Регистрация системы медиа-ассетов ---
builder.Services.AddSingleton<IStorageService, LocalStorageProvider>();
// Регистрируем основной сервис управления ассетами
builder.Services.AddScoped<IMediaAssetService, MediaAssetService>();

var storagePath = builder.Configuration["Storage:LocalPath"] ?? "media";

builder.Services.AddImageSharp()
    .Configure<PhysicalFileSystemProviderOptions>(options =>
    {
        options.ProviderRootPath = storagePath;
    })
    .Configure<PhysicalFileSystemCacheOptions>(options =>
    {
        options.CacheRootPath = Path.Combine(storagePath, "is-cache");
    });

// --- Настройка CORS (Разрешаем доступ для фронтенда) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Total-Count"); // Разрешаем видеть заголовок пагинации
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseImageSharp();
app.MapDefaultEndpoints();

// ВКЛЮЧАЕМ CORS ТУТ:
app.UseCors("AllowAll");

// Запуск миграций при старте
if (!args.Contains("ef"))
{
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
}

// Мапинг эндпоинтов
app.MapProvinceEndpoints();
app.MapCityEndpoints();
app.MapTagEndpoints();
app.MapGoogleCategoryEndpoints();

// Добавляем мапинг медиа-эндпоинтов (файл создадим следующим шагом)
app.MapMediaEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.Run();