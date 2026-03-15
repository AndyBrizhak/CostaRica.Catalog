using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Categories;

public class GoogleCategoryApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.HttpClient;

    // Вспомогательная структура для разбора ответа массового импорта
    private record BulkImportResponse(int ImportedCount);

    [Fact]
    public async Task BulkImport_ShouldCreateCategories_AndIgnoreDuplicates()
    {
        var ct = TestContext.Current.CancellationToken;

        // 1. Подготовка уникальных данных для импорта
        var gcid1 = $"cat_{Guid.NewGuid():N}";
        var gcid2 = $"cat_{Guid.NewGuid():N}";

        var importData = new List<GoogleCategoryImportDto>
        {
            new(gcid1, "Category 1", "Categoria 1"),
            new(gcid2, "Category 2", "Categoria 2")
        };

        // 2. Act: Первый импорт
        var response = await _client.PostAsJsonAsync("/api/google-categories/bulk", importData, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // ИСПРАВЛЕНО: Десериализуем в конкретный тип вместо dynamic
        var result = await response.Content.ReadFromJsonAsync<BulkImportResponse>(ct);
        result!.ImportedCount.Should().Be(2);

        // 3. Act: Повторный импорт тех же данных
        var secondResponse = await _client.PostAsJsonAsync("/api/google-categories/bulk", importData, ct);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<BulkImportResponse>(ct);

        // Должно быть 0, так как записи уже существуют
        secondResult!.ImportedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByGcid_ShouldReturnCorrectCategory()
    {
        var ct = TestContext.Current.CancellationToken;
        var gcid = $"find_by_gcid_{Guid.NewGuid():N}";

        // 1. Создаем категорию
        var createDto = new GoogleCategoryUpsertDto(gcid, "Search Target", "Objetivo");
        await _client.PostAsJsonAsync("/api/google-categories", createDto, ct);

        // 2. Act: Запрос по GCID
        var response = await _client.GetAsync($"/api/google-categories/gcid/{gcid}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.Content.ReadFromJsonAsync<GoogleCategoryResponseDto>(ct);
        category!.Gcid.Should().Be(gcid);
        category.NameEn.Should().Be("Search Target");
    }

    [Fact]
    public async Task Search_ShouldReturnPaginatedData_WithTotalCountHeader()
    {
        var ct = TestContext.Current.CancellationToken;

        // 1. Создаем уникальную запись для поиска
        var uniqueName = $"UniqueName_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/google-categories",
            new GoogleCategoryUpsertDto($"gcid_{Guid.NewGuid():N}", uniqueName, "Name Es"), ct);

        // 2. Act: Поиск по названию
        var response = await _client.GetAsync($"/api/google-categories?searchTerm={uniqueName}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Проверка заголовка для пагинации
        response.Headers.Contains("X-Total-Count").Should().BeTrue();
        var totalCount = int.Parse(response.Headers.GetValues("X-Total-Count").First());
        totalCount.Should().BeGreaterThanOrEqualTo(1);

        var items = await response.Content.ReadFromJsonAsync<List<GoogleCategoryResponseDto>>(ct);
        items.Should().Contain(c => c.NameEn == uniqueName);
    }

    [Fact]
    public async Task FullCrudCycle_ShouldWorkCorrectly()
    {
        var ct = TestContext.Current.CancellationToken;
        var gcid = $"crud_{Guid.NewGuid():N}";

        // 1. CREATE
        var createRes = await _client.PostAsJsonAsync("/api/google-categories",
            new GoogleCategoryUpsertDto(gcid, "Original", "Original"), ct);
        var created = await createRes.Content.ReadFromJsonAsync<GoogleCategoryResponseDto>(ct);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. UPDATE
        var updateDto = new GoogleCategoryUpsertDto(gcid, "Updated", "Actualizado");
        var updateRes = await _client.PutAsJsonAsync($"/api/google-categories/{created!.Id}", updateDto, ct);
        updateRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. DELETE
        var deleteRes = await _client.DeleteAsync($"/api/google-categories/{created.Id}", ct);
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. VERIFY
        var getRes = await _client.GetAsync($"/api/google-categories/{created.Id}", ct);
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}