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
        // Создаем билдер и СРАЗУ говорим ему пропустить тома
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CostaRica_Catalog_AppHost>();
        appHost.Configuration["SkipVolumes"] = "true";

        _app = await appHost.BuildAsync();

        // Запускаем всё. Теперь Aspire точно знает, что делать.
        await _app.StartAsync();

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