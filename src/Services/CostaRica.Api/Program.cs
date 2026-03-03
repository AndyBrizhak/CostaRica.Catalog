using CostaRica.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Стандартные настройки .NET Aspire
builder.AddServiceDefaults();

// Регистрация контекста базы данных для PostgreSQL
builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    // Обязательно включаем поддержку гео-данных для работы с картами
    options.UseNpgsql(o => o.UseNetTopologySuite());
});

// Настройка OpenAPI для документации
builder.Services.AddOpenApi();

var app = builder.Build();

// Маршруты мониторинга и состояния Aspire
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ПРИМЕР: Твой первый Minimal API эндпоинт с инъекцией контекста
app.MapGet("/api/provinces", async (DirectoryDbContext db) =>
{
    return await db.Provinces.ToListAsync();
})
.WithName("GetProvinces");

app.Run();