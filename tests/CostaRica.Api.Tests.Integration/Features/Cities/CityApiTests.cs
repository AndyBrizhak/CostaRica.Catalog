using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Cities;

public class CityApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task CreateCity_ShouldReturnCreated_WhenProvinceExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var pSlug = $"prov-{Guid.NewGuid().ToString()[..8]}";
        var pRes = await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto("Test Prov", pSlug), ct);
        var province = await pRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        var citySlug = $"city-{Guid.NewGuid().ToString()[..8]}";
        var cityDto = new CityUpsertDto("Test City", citySlug, province!.Id);

        var response = await client.PostAsJsonAsync("/api/cities", cityDto, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCity = await response.Content.ReadFromJsonAsync<CityResponseDto>(ct);
        createdCity!.Name.Should().Be("Test City");
        createdCity.ProvinceId.Should().Be(province.Id);
    }

    [Fact]
    public async Task GetCitiesByProvince_ShouldReturnCorrectCities()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var pSlug = $"filter-{Guid.NewGuid().ToString()[..8]}";

        var pRes = await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto("Filter Prov", pSlug), ct);
        var province = await pRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        var c1Res = await client.PostAsJsonAsync("/api/cities", new CityUpsertDto("City 1", $"c1-{pSlug}", province!.Id), ct);
        c1Res.EnsureSuccessStatusCode();

        var c2Res = await client.PostAsJsonAsync("/api/cities", new CityUpsertDto("City 2", $"c2-{pSlug}", province!.Id), ct);
        c2Res.EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/cities/province/{pSlug}", ct);

        response.EnsureSuccessStatusCode();
        var cities = await response.Content.ReadFromJsonAsync<IEnumerable<CityResponseDto>>(ct);
        cities.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteProvince_ShouldFail_WhenHasCities_RestrictBehavior()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // 1. Создаем уникальную провинцию
        var pSlug = $"restrict-{Guid.NewGuid().ToString()[..8]}";
        var pRes = await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto("Restrict Prov", pSlug), ct);
        pRes.EnsureSuccessStatusCode();
        var province = await pRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        // 2. Создаем уникальный город в этой провинции
        var citySlug = $"sticky-{Guid.NewGuid().ToString()[..8]}";
        var cRes = await client.PostAsJsonAsync("/api/cities", new CityUpsertDto("Sticky City", citySlug, province!.Id), ct);
        cRes.EnsureSuccessStatusCode(); // КРИТИЧНО: если город не создастся, тест упадет здесь

        // 3. Пытаемся удалить провинцию
        var response = await client.DeleteAsync($"/api/provinces/{province.Id}", ct);

        // Assert
        // Теперь мы точно знаем, что в провинции есть город, и БД должна запретить удаление
        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400);
    }
}