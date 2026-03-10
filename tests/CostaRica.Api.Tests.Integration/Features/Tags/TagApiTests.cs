using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Tags;

public class TagApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private async Task<Guid> CreateGroupAsync(string slug)
    {
        var client = fixture.HttpClient;
        var dto = new TagGroupUpsertDto("Test Group", "Grupo de prueba", slug);
        var response = await client.PostAsJsonAsync("/api/tag-groups", dto);
        var result = await response.Content.ReadFromJsonAsync<TagGroupResponseDto>();
        return result!.Id;
    }

    [Fact]
    public async Task CreateTag_ShouldReturnCreated_WhenGroupExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // 1. Сначала создаем родительскую группу
        var groupId = await CreateGroupAsync($"group-{Guid.NewGuid():N}");

        // 2. Создаем сам тег
        var tagSlug = $"tag-{Guid.NewGuid():N}";
        var tagDto = new TagUpsertDto("WiFi", "WiFi", tagSlug, groupId);

        var response = await client.PostAsJsonAsync("/api/tags", tagDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TagResponseDto>(ct);
        created!.Slug.Should().Be(tagSlug);
        created.TagGroupId.Should().Be(groupId);
    }

    [Fact]
    public async Task CreateTag_ShouldReturnConflict_WhenSlugAlreadyExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var groupId = await CreateGroupAsync($"group-{Guid.NewGuid():N}");
        var tagSlug = $"dup-{Guid.NewGuid():N}";
        var tagDto = new TagUpsertDto("Tag 1", "T1", tagSlug, groupId);

        // Первый раз — успех
        await client.PostAsJsonAsync("/api/tags", tagDto, ct);

        // Второй раз с тем же слагом — ожидаем 409 Conflict
        var response = await client.PostAsJsonAsync("/api/tags", tagDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetTagsByGroup_ShouldReturnOnlyRelevantTags()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var g1Id = await CreateGroupAsync($"g1-{Guid.NewGuid():N}");
        var g2Id = await CreateGroupAsync($"g2-{Guid.NewGuid():N}");

        // Добавляем теги в разные группы
        await client.PostAsJsonAsync("/api/tags", new TagUpsertDto("T1", "T1", $"t1-{Guid.NewGuid():N}", g1Id), ct);
        await client.PostAsJsonAsync("/api/tags", new TagUpsertDto("T2", "T2", $"t2-{Guid.NewGuid():N}", g2Id), ct);

        // Act: запрашиваем теги только первой группы
        var response = await client.GetAsync($"/api/tags/group/{g1Id}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tags = await response.Content.ReadFromJsonAsync<IEnumerable<TagResponseDto>>(ct);
        tags.Should().ContainSingle();
        tags!.First().TagGroupId.Should().Be(g1Id);
    }
}