using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis")
    .WithImageTag("15-3.3");

// Если в конфигурации есть флаг "SkipVolumes", не создаем том для БД. 
if (builder.Configuration["SkipVolumes"] != "true")
{
    postgres.WithDataVolume();
}

var db = postgres.AddDatabase("postgresdb");

// Запускаем API как проект (процесс)
var api = builder.AddProject<Projects.CostaRica_Api>("api")
       .WithReference(db)
       .WaitFor(db);

// --- Настройка хранилища для медиа-ассетов ---

// Так как API запускается как процесс, мы создаем обычную папку на диске хоста.
// Путь будет: /путь/к/решению/media_storage
var storagePath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../media_storage"));

if (builder.Configuration["SkipVolumes"] != "true")
{
    // Автоматически создаем папку, если её нет
    if (!Directory.Exists(storagePath))
    {
        Directory.CreateDirectory(storagePath);
    }
}

// Передаем путь в API через конфигурацию. 
// Когда вы перейдете на Docker-контейнер для API или Cloudflare R2, мы просто сменим это значение.
api.WithEnvironment("Storage__LocalPath", storagePath);

builder.Build().Run();