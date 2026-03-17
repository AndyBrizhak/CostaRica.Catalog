using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Provinces;

public class ProvinceApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task GetProvinces_ShouldReturnSuccess_WithTotalCountHeader()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var response = await client.GetAsync("/api/provinces", ct);

        response.EnsureSuccessStatusCode();

        // Проверяем наличие заголовков для react-admin
        response.Headers.Should().ContainKey("X-Total-Count");
        response.Headers.Should().ContainKey("Access-Control-Expose-Headers");
        response.Headers.GetValues("Access-Control-Expose-Headers").Should().Contain("X-Total-Count");

        var provinces = await response.Content.ReadFromJsonAsync<IEnumerable<ProvinceResponseDto>>(ct);
        provinces.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProvinces_WithPagination_ShouldReturnLimitedItems()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // Создадим несколько записей для теста пагинации
        for (int i = 0; i < 3; i++)
        {
            var slug = $"pag-{i}-{Guid.NewGuid().ToString()[..4]}";
            await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto($"Paginated {i}", slug), ct);
        }

        // Act: Запрашиваем 1-ю страницу, размер 2
        var response = await client.GetAsync("/api/provinces?page=1&pageSize=2", ct);

        // Assert
        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<List<ProvinceResponseDto>>(ct);

        provinces.Should().NotBeNull();
        // ИСПРАВЛЕНО: Правильное название метода - BeLessThanOrEqualTo
        provinces!.Count.Should().BeLessThanOrEqualTo(2);

        // Проверка заголовка
        response.Headers.GetValues("X-Total-Count").First().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProvinces_WithSearch_ShouldReturnFilteredResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var uniqueName = $"Search-{Guid.NewGuid().ToString()[..8]}";
        await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto(uniqueName, uniqueName.ToLower()), ct);

        // Act
        var response = await client.GetAsync($"/api/provinces?searchTerm={uniqueName}", ct);

        // Assert
        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<List<ProvinceResponseDto>>(ct);

        provinces.Should().ContainSingle(p => p.Name == uniqueName);
        response.Headers.GetValues("X-Total-Count").First().Should().Be("1");
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
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteProvince_ShouldReturnNoContent_WhenExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var slug = $"del-{Guid.NewGuid().ToString()[..8]}";
        var createRes = await client.PostAsJsonAsync("/api/provinces", new ProvinceUpsertDto("To Delete", slug), ct);
        var province = await createRes.Content.ReadFromJsonAsync<ProvinceResponseDto>(ct);

        // Act
        var response = await client.DeleteAsync($"/api/provinces/{province!.Id}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}