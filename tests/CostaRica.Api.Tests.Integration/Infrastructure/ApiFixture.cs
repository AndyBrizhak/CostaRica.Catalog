using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CostaRica.Api.Tests.Integration.Infrastructure;

// Этот класс отвечает за запуск ВСЕГО решения (AppHost + API + DB) один раз для группы тестов
public class ApiFixture : IAsyncDisposable
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    public HttpClient HttpClient => _httpClient ?? throw new Exception("Fixture not initialized");

    public async Task InitializeAsync()
    {
        // Создаем биbuilder для тестового запуска AppHost
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CostaRica_Catalog_AppHost>();

        // Здесь можно подменить настройки, если нужно, например:
        // appHost.Services.Configure<SomeOptions>(...)

        _app = await appHost.BuildAsync();

        // Запускаем все ресурсы (базу, API)
        await _app.StartAsync();

        // Создаем клиент, который знает, на каком порту поднялось наше API внутри Aspire
        _httpClient = _app.CreateHttpClient("api");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}