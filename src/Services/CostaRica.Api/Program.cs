using CostaRica.Api.Data;
using CostaRica.Api.Endpoints; // Добавлено
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    options.UseNpgsql(o => o.UseNetTopologySuite());
});

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DirectoryDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Регистрируем наши эндпоинты (вместо старого MapGet)
app.MapProvinceEndpoints();

app.Run();