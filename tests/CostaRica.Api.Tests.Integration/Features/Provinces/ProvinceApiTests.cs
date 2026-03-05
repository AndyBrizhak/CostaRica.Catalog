using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using FluentAssertions;
using System.Net.Http.Json;

namespace CostaRica.Api.Tests.Integration.Features.Provinces;

public class ProvinceApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    // В xUnit v3 возвращаем ValueTask вместо Task
    public async ValueTask InitializeAsync()
    {
        await fixture.InitializeAsync();
    }

    // В xUnit v3 возвращаем ValueTask
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetProvinces_ReturnsEmptyList_OnEmptyDatabase()
    {
        // Arrange
        var client = fixture.HttpClient;

        // Act
        // Используем TestContext.Current.CancellationToken для отмены запроса, если тест прерван
        var response = await client.GetAsync("/api/provinces", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<IEnumerable<ProvinceResponseDto>>(TestContext.Current.CancellationToken);

        provinces.Should().NotBeNull();
        provinces.Should().BeEmpty();
    }
}