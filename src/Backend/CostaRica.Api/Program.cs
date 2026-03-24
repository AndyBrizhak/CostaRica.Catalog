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

// --- БД: Совместимость с миграциями (Design-time) и Aspire (Runtime) ---
var isEfTooling = args.Contains("ef") || AppContext.GetData("EF_DESIGN_TIME") is true;

if (isEfTooling)
{
    builder.Services.AddDbContext<DirectoryDbContext>(options =>
    {
        options.UseNpgsql("Host=localhost;Database=unused", o => o.UseNetTopologySuite());
    });
}
else
{
    builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
    {
        options.UseNpgsql(o =>
        {
            o.UseNetTopologySuite();
            o.EnableRetryOnFailure();
        });
    });
}

// --- РЕГИСТРАЦИЯ СЕРВИСОВ ---
builder.Services.AddScoped<IProvinceService, ProvinceService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ITagGroupService, TagGroupService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IGoogleCategoryService, GoogleCategoryService>();
builder.Services.AddScoped<IMediaAssetService, MediaAssetService>();
builder.Services.AddScoped<IBusinessPageService, BusinessPageService>(); // Сервис из текущей ветки
builder.Services.AddSingleton<IStorageService, LocalStorageProvider>();

// --- IMAGESHARP (Твоя исходная конфигурация) ---
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

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Total-Count");
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- ГАРАНТИЯ НАЛИЧИЯ WWWROOT (Для ImageSharp) ---
var wwwrootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath)) Directory.CreateDirectory(wwwrootPath);

app.UseImageSharp();
app.MapDefaultEndpoints();
app.UseStaticFiles();
app.UseCors("AllowAll");

// --- SWAGGER / SCALAR ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

// --- АВТО-МИГРАЦИИ (Только в Runtime) ---
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

// --- МАППИНГ ЭНДПОИНТОВ ---
app.MapProvinceEndpoints();
app.MapCityEndpoints();
app.MapTagEndpoints();
app.MapGoogleCategoryEndpoints();
app.MapMediaEndpoints();
app.MapBusinessPageEndpoints(); // Эндпоинты из текущей ветки

app.Run();