using CostaRica.Api.Data;
using CostaRica.Api.Endpoints; // Убедись, что файл Endpoints/ProvinceEndpoints.cs создан
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Инфраструктура
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    options.UseNpgsql(o => o.UseNetTopologySuite());
});

// 2. OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// 3. Автоматические миграции
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DirectoryDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

// 4. Настройки для разработки
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // ГЛАВНОЕ: Редирект, чтобы в Aspire при клике на API сразу открывался Scalar
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

// 5. Регистрация CRUD эндпоинтов для провинций
app.MapProvinceEndpoints();

app.Run();