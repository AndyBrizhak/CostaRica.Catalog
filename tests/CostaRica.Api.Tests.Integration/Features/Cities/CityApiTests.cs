using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Cities;

public class CityApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task CreateCity_ShouldReturnFlatDtoWithProvinceName()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // 1. Создаем провинцию
        var pSlug = $"prov-{Guid.NewGuid().ToString()[..8]}";
        var pRes = await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto("Test Prov", pSlug), ct);
        var province = await pRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        // 2. Создаем город
        var citySlug = $"city-{Guid.NewGuid().ToString()[..8]}";
        var cityDto = new CityUpsertDto("Test City", citySlug, province!.Id);
        var response = await client.PostAsJsonAsync("/api/cities", cityDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCity = await response.Content.ReadFromJsonAsync<CityResponseDto>(ct);

        createdCity.Should().NotBeNull();
        createdCity!.ProvinceName.Should().Be("Test Prov"); // Проверка плоского DTO
        createdCity.ProvinceId.Should().Be(province.Id);
    }

    [Fact]
    public async Task GetAll_WithPagination_ShouldReturnCorrectItemsAndHeaders()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // Arrange: Создаем провинцию и 5 городов в ней
        var pSlug = $"pag-{Guid.NewGuid().ToString()[..8]}";
        var pRes = await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto("Pag Prov", pSlug), ct);
        var province = await pRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        for (int i = 1; i <= 5; i++)
        {
            await client.PostAsJsonAsync("/api/cities", new CityUpsertDto($"City {i}", $"c{i}-{pSlug}", province!.Id), ct);
        }

        // Act: Запрашиваем только первые 2 города этой провинции
        var response = await client.GetAsync($"/api/cities?ProvinceId={province.Id}&_start=0&_end=2", ct);

        // Assert
        response.EnsureSuccessStatusCode();

        // Проверка заголовков для react-admin
        response.Headers.Contains("X-Total-Count").Should().BeTrue();
        response.Headers.GetValues("X-Total-Count").First().Should().Be("5");
        response.Headers.GetValues("Access-Control-Expose-Headers").First().Should().Contain("X-Total-Count");

        // Проверка контента
        var items = await response.Content.ReadFromJsonAsync<IEnumerable<CityResponseDto>>(ct);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithSorting_ShouldReturnOrderedList()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var pSlug = $"sort-{Guid.NewGuid().ToString()[..8]}";
        var pRes = await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto("Sort Prov", pSlug), ct);
        var province = await pRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        await client.PostAsJsonAsync("/api/cities", new CityUpsertDto("A-City", $"a-{pSlug}", province!.Id), ct);
        await client.PostAsJsonAsync("/api/cities", new CityUpsertDto("Z-City", $"z-{pSlug}", province!.Id), ct);

        // Act: Сортировка по имени убыванию
        var response = await client.GetAsync($"/api/cities?ProvinceId={province.Id}&_sort=Name&_order=DESC", ct);

        // Assert
        var items = await response.Content.ReadFromJsonAsync<List<CityResponseDto>>(ct);
        items![0].Name.Should().Be("Z-City");
        items[1].Name.Should().Be("A-City");
    }

    [Fact]
    public async Task DeleteProvince_ShouldFail_WhenHasCities_RestrictBehavior()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var pSlug = $"restr-{Guid.NewGuid().ToString()[..8]}";
        var pRes = await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto("Restrict", pSlug), ct);
        var province = await pRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        await client.PostAsJsonAsync("/api/cities", new CityUpsertDto("Stay", $"stay-{pSlug}", province!.Id), ct);

        // Act: Пытаемся удалить провинцию
        var response = await client.DeleteAsync($"/api/provinces/{province.Id}", ct);

        // Assert: Ожидаем ошибку из-за DeleteBehavior.Restrict
        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400);
    }
}