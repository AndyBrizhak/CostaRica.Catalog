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

        var gcid1 = $"cat_{Guid.NewGuid():N}";
        var gcid2 = $"cat_{Guid.NewGuid():N}";

        var importData = new List<GoogleCategoryImportDto>
        {
            new(gcid1, "Category 1", "Categoria 1"),
            new(gcid2, "Category 2", "Categoria 2")
        };

        // Act: Первый импорт
        var response = await _client.PostAsJsonAsync("/api/google-categories/bulk", importData, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BulkImportResponse>(ct);
        result!.ImportedCount.Should().Be(2);

        // Act: Повторный импорт тех же данных (проверка защиты от дубликатов)
        var secondResponse = await _client.PostAsJsonAsync("/api/google-categories/bulk", importData, ct);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<BulkImportResponse>(ct);
        secondResult!.ImportedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAll_ShouldSupportReactAdminPaginationAndSorting()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange: Создаем несколько записей
        var prefix = Guid.NewGuid().ToString("N");
        await _client.PostAsJsonAsync("/api/google-categories/bulk", new List<GoogleCategoryImportDto>
        {
            new($"{prefix}_1", "B-Category", "B"),
            new($"{prefix}_2", "A-Category", "A"),
            new($"{prefix}_3", "C-Category", "C")
        }, ct);

        // Act: Запрос с сортировкой по NameEn и пагинацией (берем первые 2)
        var response = await _client.GetAsync($"/api/google-categories?_start=0&_end=2&_sort=NameEn&_order=ASC&q={prefix}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Проверка заголовков пагинации
        response.Headers.Contains("X-Total-Count").Should().BeTrue();
        int.Parse(response.Headers.GetValues("X-Total-Count").First()).Should().Be(3);

        var items = await response.Content.ReadFromJsonAsync<List<GoogleCategoryResponseDto>>(ct);
        items.Should().HaveCount(2);
        items![0].NameEn.Should().Be("A-Category"); // Так как сортировка по алфавиту ASC
        items[1].NameEn.Should().Be("B-Category");
    }

    [Fact]
    public async Task GetAll_ShouldSupportGetManyByIds()
    {
        var ct = TestContext.Current.CancellationToken;

        // 1. Создаем две категории
        var res1 = await _client.PostAsJsonAsync("/api/google-categories", new GoogleCategoryUpsertDto($"m1_{Guid.NewGuid():N}", "Many 1", "M1"), ct);
        var res2 = await _client.PostAsJsonAsync("/api/google-categories", new GoogleCategoryUpsertDto($"m2_{Guid.NewGuid():N}", "Many 2", "M2"), ct);

        var cat1 = await res1.Content.ReadFromJsonAsync<GoogleCategoryResponseDto>(ct);
        var cat2 = await res2.Content.ReadFromJsonAsync<GoogleCategoryResponseDto>(ct);

        // 2. Act: Запрашиваем только их через массив id (имитация поведения React Admin)
        var response = await _client.GetAsync($"/api/google-categories?id={cat1!.Id}&id={cat2!.Id}", ct);

        // Assert
        var items = await response.Content.ReadFromJsonAsync<List<GoogleCategoryResponseDto>>(ct);
        items.Should().HaveCount(2);
        items.Should().Contain(c => c.Id == cat1.Id);
        items.Should().Contain(c => c.Id == cat2.Id);
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
        var updateDto = new GoogleCategoryUpsertDto($"{gcid}_upd", "Updated", "Actualizado");
        var updateRes = await _client.PutAsJsonAsync($"/api/google-categories/{created!.Id}", updateDto, ct);
        updateRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. GET BY GCID (проверка обновления)
        var getByGcidRes = await _client.GetAsync($"/api/google-categories/gcid/{gcid}_upd", ct);
        getByGcidRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. DELETE
        await _client.DeleteAsync($"/api/google-categories/{created.Id}", ct);

        // 5. VERIFY 404
        var finalGet = await _client.GetAsync($"/api/google-categories/{created.Id}", ct);
        finalGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}