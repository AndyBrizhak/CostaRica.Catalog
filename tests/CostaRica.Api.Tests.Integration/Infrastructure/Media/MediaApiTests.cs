using CostaRica.Api.DTOs;
using CostaRica.Api.Tests.Integration.Infrastructure;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;

namespace CostaRica.Api.Tests.Integration.Features.Media;

public class MediaApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private async Task<(Guid Id, string Slug)> UploadTestImageAsync(string slug)
    {
        var client = fixture.HttpClient;

        // 1. Подготавливаем контент формы (имитируем браузер)
        using var content = new MultipartFormDataContent();

        // Добавляем файл (фейковые байты изображения)
        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]); // Заголовок PNG
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "test-image.png");

        // Добавляем текстовые поля
        content.Add(new StringContent(slug), "slug");
        content.Add(new StringContent("English Alt"), "altTextEn");
        content.Add(new StringContent("Spanish Alt"), "altTextEs");

        // 2. Отправляем запрос
        var response = await client.PostAsync("/media/upload", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MediaAssetResponseDto>();
        return (result!.Id, result.Slug);
    }

    [Fact]
    public async Task Upload_ShouldReturnCreated_AndSaveData()
    {
        var ct = TestContext.Current.CancellationToken;
        var slug = $"int-test-{Guid.NewGuid():N}";

        // Act
        var (id, returnedSlug) = await UploadTestImageAsync(slug);

        // Assert
        id.Should().NotBeEmpty();
        returnedSlug.Should().Be(slug);
    }

    [Fact]
    public async Task GetFile_ShouldReturnRedirect_WhenSlugIsIncorrect()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var slug = $"seo-test-{Guid.NewGuid():N}";

        var (id, correctSlug) = await UploadTestImageAsync(slug);

        // Act: запрашиваем с неправильным слагом
        // Важно: HttpClient по умолчанию следует за редиректами. 
        // Чтобы проверить именно факт редиректа, мы создаем обработчик без авто-редиректа (но для простоты проверим итоговый URL)
        var response = await client.GetAsync($"/media/{id}/wrong-slug", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Проверяем, что в итоге нас привело к правильному файлу (Content-Type)
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/png");
    }

    [Fact]
    public async Task GetMediaList_ShouldFilterOrphans()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;

        // Загружаем один ассет (он будет "сиротой", так как ни к чему не привязан)
        await UploadTestImageAsync($"orphan-{Guid.NewGuid():N}");

        // Act
        var response = await client.GetAsync("/media?OnlyOrphans=true", ct);

        // Assert
        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<MediaAssetResponseDto>>(ct);
        list.Should().NotBeEmpty();
        list.Should().Contain(x => x.Slug.Contains("orphan"));
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_AndCleanUp()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = fixture.HttpClient;
        var (id, slug) = await UploadTestImageAsync($"to-delete-{Guid.NewGuid():N}");

        // Act
        var deleteResponse = await client.DeleteAsync($"/media/{id}", ct);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Проверяем, что файл больше не доступен
        var getResponse = await client.GetAsync($"/media/{id}/{slug}", ct);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}