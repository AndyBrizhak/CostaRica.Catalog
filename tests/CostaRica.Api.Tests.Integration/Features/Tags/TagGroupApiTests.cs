using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Tags;

public class TagGroupApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task GetTagGroups_ShouldReturnPaginationHeadersAndFilteredResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // Изолируем данные теста с помощью уникального префикса
        var prefix = $"OrderTest-{Guid.NewGuid().ToString()[..8]}";

        await client.PostAsJsonAsync("/api/tag-groups", new TagGroupUpsertDto($"{prefix}-Alpha", "A", $"{prefix}-a"), ct);
        await client.PostAsJsonAsync("/api/tag-groups", new TagGroupUpsertDto($"{prefix}-Beta", "B", $"{prefix}-b"), ct);
        await client.PostAsJsonAsync("/api/tag-groups", new TagGroupUpsertDto($"{prefix}-Gamma", "G", $"{prefix}-g"), ct);

        // Act: Запрашиваем только наши тестовые данные (через Q), проверяя пагинацию и сортировку
        var response = await client.GetAsync($"/api/tag-groups?Q={prefix}&_start=0&_end=2&_sort=NameEn&_order=DESC", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 1. Проверка заголовков react-admin
        response.Headers.Contains("X-Total-Count").Should().BeTrue();
        // Мы создали 3, значит Total должен быть 3
        response.Headers.GetValues("X-Total-Count").First().Should().Be("3");
        response.Headers.GetValues("Access-Control-Expose-Headers").Should().Contain("X-Total-Count");

        // 2. Проверка содержимого (сортировка DESC: Gamma -> Beta -> Alpha)
        var items = await response.Content.ReadFromJsonAsync<List<TagGroupResponseDto>>(ct);
        items.Should().HaveCount(2);
        items![0].NameEn.Should().Be($"{prefix}-Gamma");
        items![1].NameEn.Should().Be($"{prefix}-Beta");
    }

    [Fact]
    public async Task GetTagGroups_WithGlobalSearch_ShouldReturnCorrectItems()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var uniqueKey = Guid.NewGuid().ToString()[..8];
        await client.PostAsJsonAsync("/api/tag-groups", new TagGroupUpsertDto($"Searchable-{uniqueKey}", "Encontrable", $"search-{uniqueKey}"), ct);

        // Act: Ищем по уникальному ключу
        var response = await client.GetAsync($"/api/tag-groups?Q={uniqueKey}", ct);

        // Assert
        var items = await response.Content.ReadFromJsonAsync<List<TagGroupResponseDto>>(ct);
        items.Should().ContainSingle(x => x.NameEn.Contains(uniqueKey));
    }

    [Fact]
    public async Task DeleteTagGroup_WhenHasTags_ShouldReturnNotFound_InsteadOfInternalError()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var groupRes = await client.PostAsJsonAsync("/api/tag-groups",
            new TagGroupUpsertDto("Parent", "P", $"parent-{Guid.NewGuid():N}"), ct);
        var group = await groupRes.Content.ReadFromJsonAsync<TagGroupResponseDto>(ct);

        await client.PostAsJsonAsync("/api/tags",
            new TagUpsertDto("Child", "C", $"child-{Guid.NewGuid():N}", group!.Id), ct);

        // Act
        var deleteRes = await client.DeleteAsync($"/api/tag-groups/{group.Id}", ct);

        // Assert
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTagGroup_WithDuplicateSlug_ShouldReturnConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var slug1 = $"s1-{Guid.NewGuid():N}";
        var slug2 = $"s2-{Guid.NewGuid():N}";

        await client.PostAsJsonAsync("/api/tag-groups", new TagGroupUpsertDto("G1", "G1", slug1), ct);
        var res2 = await client.PostAsJsonAsync("/api/tag-groups", new TagGroupUpsertDto("G2", "G2", slug2), ct);
        var g2 = await res2.Content.ReadFromJsonAsync<TagGroupResponseDto>(ct);

        // Act
        var updateDto = new TagGroupUpsertDto("G2-Updated", "G2-U", slug1);
        var response = await client.PutAsJsonAsync($"/api/tag-groups/{g2!.Id}", updateDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}