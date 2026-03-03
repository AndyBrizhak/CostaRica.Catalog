using CostaRica.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<DirectoryDbContext>("postgresdb", configureDbContextOptions: options =>
{
    options.UseNpgsql(o => o.UseNetTopologySuite());
});

builder.Services.AddOpenApi();

var app = builder.Build();

// --- ДОБАВЛЕННЫЙ БЛОК ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DirectoryDbContext>();
    await db.Database.MigrateAsync();
}
// ------------------------

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/provinces", async (DirectoryDbContext db) =>
{
    return await db.Provinces.ToListAsync();
})
.WithName("GetProvinces");

app.Run();