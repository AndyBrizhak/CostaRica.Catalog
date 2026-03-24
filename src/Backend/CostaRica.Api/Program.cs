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

// --- РЕГИСТРАЦИЯ КОНТЕКСТА БД (С поддержкой миграций вне Aspire) ---
var isEfTooling = args.Contains("ef") || AppContext.GetData("EF_DESIGN_TIME") is true;

if (isEfTooling)
{
    // Режим инструментов миграции: используем стандартную регистрацию с фиктивной строкой
    builder.Services.AddDbContext<DirectoryDbContext>(options =>
    {
        options.UseNpgsql("Host=localhost;Database=unused", o =>
        {
            o.UseNetTopologySuite();
        });
    });
}
else
{
    // Обычный режим: используем регистрацию через Aspire
    builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
    {
        options.UseNpgsql(o =>
        {
            o.UseNetTopologySuite();
            o.EnableRetryOnFailure();
        });
    });
}

// Регистрация бизнес-сервисов
builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ITagGroupService, TagGroupService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IGoogleCategoryService, GoogleCategoryService>();
builder.Services.AddScoped<IBusinessPageService, BusinessPageService>();

// Регистрация системы медиа-ассетов
builder.Services.AddScoped<IMediaAssetService, MediaAssetService>();
builder.Services.AddSingleton<IStorageService, LocalStorageProvider>();

// Настройка обработки изображений ImageSharp
builder.Services.AddImageSharp()
    .SetCache<PhysicalFileSystemCache>()
    .AddProvider<PhysicalFileSystemProvider>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Настройка CORS для react-admin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("X-Total-Count");
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.UseImageSharp();
app.UseStaticFiles();
app.UseCors("AllowAll");

// Запуск миграций при старте (только если это не запуск инструментов миграции)
if (!isEfTooling)
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
app.MapMediaEndpoints();
app.MapBusinessPageEndpoints();

app.Run();