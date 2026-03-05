using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;

namespace CostaRica.Api.Tests.Integration.Features.Provinces;

public class ProvinceApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task GetProvinces_ReturnsSuccess()
    {
        var client = fixture.HttpClient;
        var response = await client.GetAsync("/api/provinces", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateProvince_ShouldWork()
    {
        var client = fixture.HttpClient;
        // Используем случайную строку, чтобы тест был атомарным
        var name = $"Test {Guid.NewGuid().ToString()[..5]}";
        var slug = $"slug-{Guid.NewGuid().ToString()[..5]}";

        var dto = new ProvinceUpsertDto(name, slug);

        var response = await client.PostAsJsonAsync("/api/provinces", dto, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }
}