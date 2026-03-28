using Aspire.Hosting;
using Aspire.Hosting.Testing;
using System.Net.Http.Json;
using System.Net.Http.Headers;

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

        // 2. Отключаем тома для изоляции (каждый запуск — чистая база)
        appHost.Configuration["SkipVolumes"] = "true";

        _app = await appHost.BuildAsync();

        // 3. Запускаем оркестратор Aspire
        await _app.StartAsync();

        // 4. Ждем готовности API (Health Check)
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api");

        // 5. Создаем клиент
        _httpClient = _app.CreateHttpClient("api");

        // 6. АВТОРИЗАЦИЯ: Входим в систему как SuperAdmin
        // Данные берем из вашего файла Provinces.http
        var loginPayload = new
        {
            email = "admin@example.com",
            password = "ComplexPassword123!"
        };

        var authResponse = await _httpClient.PostAsJsonAsync("/api/auth/login", loginPayload);

        if (!authResponse.IsSuccessStatusCode)
        {
            var errorContent = await authResponse.Content.ReadAsStringAsync();
            throw new Exception($"Не удалось авторизовать тестовый клиент: {authResponse.StatusCode}. {errorContent}");
        }

        var result = await authResponse.Content.ReadFromJsonAsync<AuthResponse>();

        if (result == null || string.IsNullOrEmpty(result.token))
        {
            throw new Exception("Авторизация прошла успешно, но токен пуст.");
        }

        // Устанавливаем Bearer токен для ВСЕХ последующих запросов в интеграционных тестах
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    // Вспомогательная структура для парсинга токена
    private record AuthResponse(string token);
}