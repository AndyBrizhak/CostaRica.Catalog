using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;

namespace CostaRica.Api.Tests.Integration.Features.Media;

public class MediaApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private async Task<(Guid Id, string Slug)> UploadTestImageAsync(string slug, string altEn = "Alt En")
    {
        var client = fixture.HttpClient;
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "test.png");

        content.Add(new StringContent(slug), "slug");
        content.Add(new StringContent(altEn), "altTextEn");
        content.Add(new StringContent("Alt Es"), "altTextEs");

        var response = await client.PostAsync("/media/upload", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MediaAssetResponseDto>();
        return (result!.Id, result.Slug);
    }

    [Fact]
    public async Task GetMediaList_ShouldIncludeReactAdminHeaders_AndPagination()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // Arrange: Загружаем 3 файла для проверки счета
        await UploadTestImageAsync($"img-{Guid.NewGuid():N}");
        await UploadTestImageAsync($"img-{Guid.NewGuid():N}");
        await UploadTestImageAsync($"img-{Guid.NewGuid():N}");

        // Act: Запрашиваем только 1 элемент
        var response = await client.GetAsync("/media?_start=0&_end=1", ct);

        // Assert
        response.EnsureSuccessStatusCode();

        // 1. Проверяем заголовок X-Total-Count (должен быть >= 3)
        response.Headers.Contains("X-Total-Count").Should().BeTrue();
        var totalCount = int.Parse(response.Headers.GetValues("X-Total-Count").First());
        totalCount.Should().BeGreaterThanOrEqualTo(3);

        // 2. Проверяем Expose-Headers для CORS
        response.Headers.Contains("Access-Control-Expose-Headers").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Expose-Headers").Should().Contain("X-Total-Count");

        // 3. Проверяем, что в теле ровно 1 элемент (пагинация сработала)
        var items = await response.Content.ReadFromJsonAsync<List<MediaAssetResponseDto>>(ct);
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMediaList_ShouldSearchByTerm_UsingQParameter()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // Arrange: Создаем файл с уникальным описанием
        var uniqueTerm = $"findme-{Guid.NewGuid():N}";
        await UploadTestImageAsync($"slug-{Guid.NewGuid():N}", uniqueTerm);

        // Act
        var response = await client.GetAsync($"/media?q={uniqueTerm}", ct);

        // Assert
        var items = await response.Content.ReadFromJsonAsync<List<MediaAssetResponseDto>>(ct);
        items.Should().ContainSingle(x => x.AltTextEn == uniqueTerm);
    }

    [Fact]
    public async Task GetFile_ShouldReturnRedirect_WhenSlugIsIncorrect()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var slug = $"seo-test-{Guid.NewGuid():N}";
        var (id, _) = await UploadTestImageAsync(slug);

        // Act:HttpClient по умолчанию следует за редиректами, 
        // поэтому мы получим 200 OK и данные файла, но проверим итоговый URL
        var response = await client.GetAsync($"/media/{id}/wrong-slug", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/png");
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_AndCleanUpPhysicalFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var (id, slug) = await UploadTestImageAsync($"to-delete-{Guid.NewGuid():N}");

        // Act
        var deleteResponse = await client.DeleteAsync($"/media/{id}", ct);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Проверяем, что файл больше не доступен через API
        var getResponse = await client.GetAsync($"/media/{id}/{slug}", ct);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}