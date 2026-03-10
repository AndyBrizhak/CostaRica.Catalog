using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace CostaRica.Api.Tests.Integration.Features.Tags;

public class TagGroupApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task CreateTagGroup_ShouldReturnCreated_AndThenHandleDuplicateAsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var slug = $"group-{Guid.NewGuid().ToString()[..8]}";
        var dto = new TagGroupUpsertDto("Amenities", "Comodidades", slug);

        // 1. Успешное создание
        var response = await client.PostAsJsonAsync("/api/tag-groups", dto, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<TagGroupResponseDto>(ct);
        created!.Slug.Should().Be(slug);

        // 2. Попытка создания дубликата (проверка нашей логики Conflict/409)
        var duplicateResponse = await client.PostAsJsonAsync("/api/tag-groups", dto, ct);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetTagGroupBySlug_ShouldReturnSuccess_WhenExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var slug = $"slug-{Guid.NewGuid().ToString()[..8]}";

        await client.PostAsJsonAsync("/api/tag-groups",
            new TagGroupUpsertDto("Test Group", "Grupo de prueba", slug), ct);

        // Act
        var response = await client.GetAsync($"/api/tag-groups/slug/{slug}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var group = await response.Content.ReadFromJsonAsync<TagGroupResponseDto>(ct);
        group!.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task UpdateTagGroup_ShouldReturnOk_AndReflectChanges()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // Arrange
        var createRes = await client.PostAsJsonAsync("/api/tag-groups",
            new TagGroupUpsertDto("Original", "Original", $"upd-{Guid.NewGuid().ToString()[..8]}"), ct);
        var original = await createRes.Content.ReadFromJsonAsync<TagGroupResponseDto>(ct);

        var updateDto = new TagGroupUpsertDto("Updated Name", "Nombre Actualizado", original!.Slug);

        // Act
        var response = await client.PutAsJsonAsync($"/api/tag-groups/{original.Id}", updateDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TagGroupResponseDto>(ct);
        updated!.NameEn.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteTagGroup_ShouldReturnNoContent_AndThenNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        var createRes = await client.PostAsJsonAsync("/api/tag-groups",
            new TagGroupUpsertDto("To Delete", "Para borrar", $"del-{Guid.NewGuid().ToString()[..8]}"), ct);
        var group = await createRes.Content.ReadFromJsonAsync<TagGroupResponseDto>(ct);

        // Act & Assert
        var deleteRes = await client.DeleteAsync($"/api/tag-groups/{group!.Id}", ct);
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRes = await client.GetAsync($"/api/tag-groups/{group.Id}", ct);
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}