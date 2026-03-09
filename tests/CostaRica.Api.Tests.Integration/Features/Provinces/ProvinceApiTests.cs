using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Provinces;

public class ProvinceApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task GetProvinces_ReturnsSuccess()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var response = await client.GetAsync("/api/provinces", ct);

        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<IEnumerable<ProvinceResponseDto>>(ct);
        provinces.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAndGetBySlug_ShouldWorkCorrectly()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var slug = $"slug-{Guid.NewGuid().ToString()[..8]}";
        var dto = new ProvinceUpsertDto("Test Province", slug);

        // Создание
        var createResponse = await client.PostAsJsonAsync("/api/provinces", dto, ct);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Получение по Slug
        var getResponse = await client.GetAsync($"/api/provinces/slug/{slug}", ct);

        getResponse.EnsureSuccessStatusCode();
        var province = await getResponse.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);
        province.Should().NotBeNull();
        province!.Slug.Should().Be(slug);
        province.Name.Should().Be("Test Province");
    }

    [Fact]
    public async Task GetProvince_WithIncludeCities_ShouldReturnCitiesCollection()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var provinceSlug = $"inc-{Guid.NewGuid().ToString()[..8]}";
        var pRes = await client.PostAsJsonAsync("/api/provinces",
            new ProvinceUpsertDto("Include Test", provinceSlug), ct);
        var province = await pRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        // Добавим город, чтобы проверить реальную подгрузку
        await client.PostAsJsonAsync("/api/cities",
            new CityUpsertDto("Test City", $"city-{provinceSlug}", province!.Id), ct);

        // Act: Запрашиваем с городами
        var response = await client.GetAsync($"/api/provinces/slug/{provinceSlug}?includeCities=true", ct);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);
        result.Should().NotBeNull();
        result!.Cities.Should().NotBeNull();
        result.Cities.Should().NotBeEmpty(); // Проверяем, что город действительно подтянулся
    }

    [Fact]
    public async Task DeleteProvince_ReturnsNotFound_WhenIdInvalid()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var response = await client.DeleteAsync($"/api/provinces/{Guid.NewGuid()}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}