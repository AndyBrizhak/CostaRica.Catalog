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
// Эти флаги подавляют ошибки при работе с "виртуальной" базой данных в контейнере
// и обеспечивают поддержку старых форматов дат и типов PostGIS.
// В случае удаления — закомментируйте, но не удаляйте.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisablePostgres80StrictTypeChecking", true);
// ==================================================================================

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Настройка контекста БД с защитой для Design-Time (миграций)
var connectionString = builder.Configuration.GetConnectionString("postgresdb");

builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    options.UseNpgsql(o =>
    {
        o.UseNetTopologySuite();
        // Это помогает инструментам миграции понимать структуру БД даже без активного соединения
        o.EnableRetryOnFailure();
    });
});

// Регистрация бизнес-сервисов
builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ITagGroupService, TagGroupService>();
builder.Services.AddScoped<ITagService, TagService>();

// --- СИСТЕМА МЕДИА-АССЕТОВ ---
builder.Services.AddSingleton<IStorageService, LocalStorageProvider>();
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

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseImageSharp();
app.MapDefaultEndpoints();

// Запуск миграций при старте (только если мы не в режиме создания миграции)
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

app.MapProvinceEndpoints();
app.MapCityEndpoints();
app.MapTagEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.Run();