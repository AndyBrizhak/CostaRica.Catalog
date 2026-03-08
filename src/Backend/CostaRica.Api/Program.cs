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
builder.Services.AddScoped<ICityService, CityService>(); // Регистрация сервиса городов

// 4. Генерация OpenAPI документации
builder.Services.AddOpenApi();

var app = builder.Build();

// 5. Автоматическое применение миграций при старте
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DirectoryDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

// 6. Настройки среды разработки
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Используем Scalar вместо Swagger

    // Редирект на документацию для удобства в Aspire Dashboard
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

// 7. РЕГИСТРАЦИЯ ЭНДПОИНТОВ
app.MapProvinceEndpoints();
app.MapCityEndpoints(); // Регистрация эндпоинтов городов

app.Run();