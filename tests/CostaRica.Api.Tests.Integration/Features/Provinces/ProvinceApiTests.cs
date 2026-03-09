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
        // Arrange
        var client = fixture.HttpClient;

        // Act
        var response = await client.GetAsync("/api/provinces");

        // Assert
        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<IEnumerable<ProvinceResponseDto>>();
        provinces.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAndGetBySlug_ShouldWorkCorrectly()
    {
        // Arrange
        var client = fixture.HttpClient;
        var slug = $"test-slug-{Guid.NewGuid().ToString()[..8]}";
        var dto = new ProvinceUpsertDto("Test Province", slug);

        // Act 1: Создание
        var createResponse = await client.PostAsJsonAsync("/api/provinces", dto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act 2: Получение по Slug
        var getResponse = await client.GetAsync($"/api/provinces/slug/{slug}");

        // Assert
        getResponse.EnsureSuccessStatusCode();
        var province = await getResponse.Content.ReadFromJsonAsync<ProvinceResponseDto>();
        province.Should().NotBeNull();
        province!.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task GetProvince_WithIncludeCities_ShouldReturnCitiesArray()
    {
        // Arrange
        var client = fixture.HttpClient;
        // Создаем провинцию
        var provinceSlug = $"inc-cities-{Guid.NewGuid().ToString()[..8]}";
        var createRes = await client.PostAsJsonAsync("/api/provinces",
            new ProvinceUpsertDto("Include Cities Test", provinceSlug));
        var createdProvince = await createRes.Content.ReadFromJsonAsync<ProvinceResponseDto>();

        // Act: Запрашиваем с флагом includeCities=true
        var response = await client.GetAsync($"/api/provinces/slug/{provinceSlug}?includeCities=true");

        // Assert
        response.EnsureSuccessStatusCode();
        var province = await response.Content.ReadFromJsonAsync<ProvinceResponseDto>();
        province.Should().NotBeNull();
        province!.Cities.Should().NotBeNull(); // Поле должно присутствовать (даже если пустое)
    }

    [Fact]
    public async Task CreateProvince_ReturnsConflict_WhenSlugExists()
    {
        // Arrange
        var client = fixture.HttpClient;
        var slug = $"conflict-{Guid.NewGuid().ToString()[..8]}";
        var dto = new ProvinceUpsertDto("Original", slug);

        await client.PostAsJsonAsync("/api/provinces", dto);

        // Act: Повторное создание с тем же слагом
        var response = await client.PostAsJsonAsync("/api/provinces", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteProvince_ReturnsNotFound_WhenIdIsInvalid()
    {
        // Arrange
        var client = fixture.HttpClient;

        // Act
        var response = await client.DeleteAsync($"/api/provinces/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}