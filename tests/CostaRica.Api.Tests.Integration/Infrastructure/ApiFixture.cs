using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace CostaRica.Api.Tests.Integration.Infrastructure;

public class ApiFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    public HttpClient HttpClient => _httpClient ?? throw new Exception("Fixture not initialized");

    public async ValueTask InitializeAsync()
    {
        // 1. Создаем билдер для тестового окружения
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CostaRica_Catalog_AppHost>();

        // 2. Отключаем тома для изоляции тестов (база будет создаваться с нуля в контейнере)
        appHost.Configuration["SkipVolumes"] = "true";

        _app = await appHost.BuildAsync();

        // 3. Запускаем оркестратор Aspire
        await _app.StartAsync();

        // 4. ЖДЕМ ГОТОВНОСТИ: Тесты не пойдут дальше, пока API не станет "Healthy"
        // Это предотвращает зависания и конфликты при повторных запусках
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api");

        // 5. Создаем клиент только после того, как приложение полностью готово
        _httpClient = _app.CreateHttpClient("api");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            // Корректная остановка всех контейнеров Aspire
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}