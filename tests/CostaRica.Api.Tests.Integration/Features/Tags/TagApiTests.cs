using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Tags;

public class TagApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private async Task<Guid> CreateGroupAsync(string name, string slug)
    {
        var client = fixture.HttpClient;
        var dto = new TagGroupUpsertDto(name, name, slug);
        var response = await client.PostAsJsonAsync("/api/tag-groups", dto);
        var result = await response.Content.ReadFromJsonAsync<TagGroupResponseDto>();
        return result!.Id;
    }

    [Fact]
    public async Task GetTags_ShouldFilterByTagGroupId_AndReturnHeaders()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // 1. Создаем две разные группы
        var prefix = $"FilterTest-{Guid.NewGuid().ToString()[..8]}";
        var g1Id = await CreateGroupAsync($"{prefix}-G1", $"{prefix}-g1");
        var g2Id = await CreateGroupAsync($"{prefix}-G2", $"{prefix}-g2");

        // 2. Добавляем теги: 2 в первую группу, 1 во вторую
        await client.PostAsJsonAsync("/api/tags", new TagUpsertDto($"{prefix}-T1", "T1", $"{prefix}-t1", g1Id), ct);
        await client.PostAsJsonAsync("/api/tags", new TagUpsertDto($"{prefix}-T2", "T2", $"{prefix}-t2", g1Id), ct);
        await client.PostAsJsonAsync("/api/tags", new TagUpsertDto($"{prefix}-T3", "T3", $"{prefix}-t3", g2Id), ct);

        // 3. Act: Запрашиваем теги только для первой группы
        var response = await client.GetAsync($"/api/tags?TagGroupId={g1Id}&_start=0&_end=10", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Проверка заголовков
        response.Headers.Contains("X-Total-Count").Should().BeTrue();
        response.Headers.GetValues("X-Total-Count").First().Should().Be("2");
        response.Headers.GetValues("Access-Control-Expose-Headers").Should().Contain("X-Total-Count");

        // Проверка данных
        var items = await response.Content.ReadFromJsonAsync<List<TagResponseDto>>(ct);
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(t => t.TagGroupId.Should().Be(g1Id));
    }

    [Fact]
    public async Task GetTags_WithPaginationAndSorting_ShouldWorkCorrectly()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var prefix = $"PageTest-{Guid.NewGuid().ToString()[..8]}";
        var groupId = await CreateGroupAsync($"{prefix}-GP", $"{prefix}-gp");

        // Создаем 3 тега
        await client.PostAsJsonAsync("/api/tags", new TagUpsertDto("Apple", "A", $"{prefix}-apple", groupId), ct);
        await client.PostAsJsonAsync("/api/tags", new TagUpsertDto("Cherry", "C", $"{prefix}-cherry", groupId), ct);
        await client.PostAsJsonAsync("/api/tags", new TagUpsertDto("Banana", "B", $"{prefix}-banana", groupId), ct);

        // Act: Сортируем по имени DESC и берем первые 2 (ожидаем Cherry, Banana)
        var response = await client.GetAsync($"/api/tags?Q={prefix}&_start=0&_end=2&_sort=NameEn&_order=DESC", ct);

        // Assert
        var items = await response.Content.ReadFromJsonAsync<List<TagResponseDto>>(ct);
        items.Should().HaveCount(2);
        items![0].NameEn.Should().Be("Cherry");
        items![1].NameEn.Should().Be("Banana");
        response.Headers.GetValues("X-Total-Count").First().Should().Be("3");
    }

    [Fact]
    public async Task CreateTag_WithDuplicateSlug_ShouldReturnConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var prefix = $"Conflict-{Guid.NewGuid().ToString()[..8]}";
        var groupId = await CreateGroupAsync($"{prefix}-G", $"{prefix}-g");
        var slug = $"{prefix}-dup";

        var dto = new TagUpsertDto("Original", "O", slug, groupId);

        // 1. Успешное создание
        var res1 = await client.PostAsJsonAsync("/api/tags", dto, ct);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Act: Повторное создание с тем же слагом
        var res2 = await client.PostAsJsonAsync("/api/tags", dto, ct);

        // Assert
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTag_InNonExistentGroup_ShouldReturnConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var dto = new TagUpsertDto("Orphan", "O", "orphan-tag", Guid.NewGuid()); // Случайный ID группы

        // Act
        var response = await client.PostAsJsonAsync("/api/tags", dto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}