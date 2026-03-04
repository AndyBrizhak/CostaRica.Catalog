var builder = DistributedApplication.CreateBuilder(args);

// Возвращаемся к автоматическим портам (удаляем WithEndpoint)
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis")
    .WithImageTag("15-3.3")
    .WithDataVolume();

// Фиксируем порт 5435, чтобы не искать его каждый раз в логах
//var postgres = builder.AddPostgres("postgres")
//    .WithImage("postgis/postgis")
//    .WithImageTag("15-3.3")
//    .WithDataVolume()
//    .WithEndpoint(port: 5435, targetPort: 5432, name: "postgres");

// Важно: оставляем имя "postgresdb", так как API ищет именно его
var db = postgres.AddDatabase("postgresdb");

// Подключаем проект API
builder.AddProject<Projects.CostaRica_Api>("api")
       .WithReference(db)
       // ЭТОТ МЕТОД ГОВОРИТ API ЖДАТЬ, ПОКА БАЗА НЕ СТАНЕТ HEALTHY
       .WaitFor(db);

builder.Build().Run();