var builder = DistributedApplication.CreateBuilder(args);

// Добавляем контейнер PostgreSQL с расширением PostGIS
// БЫЛО: var postgres = builder.AddPostgres("postgres")
//          .WithImage("postgis/postgis")
//          .WithImageTag("15-3.3")
//          .WithDataVolume()
//          .AddDatabase("catalogdb");

// СТАЛО (с фиксацией порта для миграций):
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis")
    .WithImageTag("15-3.3")
    .WithDataVolume()
    .WithEndpoint(port: 5432, targetPort: 5432, name: "postgres"); // Фиксируем порт, чтобы PMC мог подключиться к localhost:5432

// Создаем базу с именем, которое ожидает наш API ("postgresdb")
var db = postgres.AddDatabase("postgresdb");

// Подключаем проект API и передаем ему ссылку на базу данных
builder.AddProject<Projects.CostaRica_Api>("api")
       .WithReference(db);

builder.Build().Run();