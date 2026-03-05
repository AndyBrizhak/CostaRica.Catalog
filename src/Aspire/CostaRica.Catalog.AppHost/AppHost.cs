var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis")
    .WithImageTag("15-3.3");

// Если в конфигурации есть флаг "SkipVolumes", не создаем том. 
// Это позволит тестам работать в памяти, а вам при разработке — сохранять данные.
if (builder.Configuration["SkipVolumes"] != "true")
{
    postgres.WithDataVolume();
}

var db = postgres.AddDatabase("postgresdb");

builder.AddProject<Projects.CostaRica_Api>("api")
       .WithReference(db)
       .WaitFor(db);

builder.Build().Run();