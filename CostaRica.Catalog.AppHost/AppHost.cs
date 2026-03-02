var builder = DistributedApplication.CreateBuilder(args);

// Добавляем контейнер PostgreSQL с расширением PostGIS
// Это позволит нам использовать гео-поиск для бизнесов в Коста-Рике
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis") // Используем образ с предустановленным PostGIS
    .WithImageTag("15-3.3")       // Стабильная версия
    .WithDataVolume()             // Данные не пропадут после выключения Docker
    .AddDatabase("catalogdb");

// Здесь позже появятся ссылки на API и Web:
// builder.AddProject<Projects.CostaRica_Api>("api").WithReference(postgres);

builder.Build().Run();