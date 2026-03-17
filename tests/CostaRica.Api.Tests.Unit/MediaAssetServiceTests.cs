using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NetTopologySuite.Geometries;

namespace CostaRica.Api.Tests.Unit;

public class MediaAssetServiceTests
{
    private readonly DirectoryDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<MediaAssetService> _logger;
    private readonly MediaAssetService _service;

    public MediaAssetServiceTests()
    {
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _storageService = Substitute.For<IStorageService>();
        _logger = Substitute.For<ILogger<MediaAssetService>>();

        _service = new MediaAssetService(_context, _storageService, _logger);
    }

    [Fact]
    public async Task UploadAsync_ShouldSaveToDatabaseAndStorage_WhenDataIsValid()
    {
        // Arrange
        var stream = new MemoryStream([1, 2, 3]);
        var dto = new MediaUploadDto("test-slug", "Alt En", "Alt Es");
        var fileName = "image.png";
        var contentType = "image/png";

        // Act
        var result = await _service.UploadAsync(stream, fileName, contentType, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("test-slug");

        var inDb = await _context.MediaAssets.FindAsync(result.Id);
        inDb.Should().NotBeNull();

        await _storageService.Received(1).SaveAsync(Arg.Any<Stream>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenAssetIsLinkedToBusiness()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var asset = new MediaAsset
        {
            Id = assetId,
            Slug = "linked-asset",
            FileName = "file.jpg"
        };

        var businessPage = new BusinessPage
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            Slug = "test-biz",
            ProvinceId = Guid.NewGuid(),
            Location = new Point(0, 0) { SRID = 4326 }
        };

        asset.BusinessPages.Add(businessPage);

        _context.MediaAssets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(assetId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("используется в");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyOrphans_WhenOnlyOrphansIsTrue()
    {
        // Arrange
        var linked = new MediaAsset { Id = Guid.NewGuid(), Slug = "linked", FileName = "1.jpg" };
        linked.BusinessPages.Add(new BusinessPage
        {
            Id = Guid.NewGuid(),
            Name = "B1",
            Slug = "s1",
            ProvinceId = Guid.NewGuid(),
            Location = new Point(0, 0) { SRID = 4326 }
        });

        var orphan = new MediaAsset { Id = Guid.NewGuid(), Slug = "orphan-asset", FileName = "2.jpg" };

        _context.MediaAssets.AddRange(linked, orphan);
        await _context.SaveChangesAsync();

        var parameters = new MediaQueryParameters(onlyOrphans: true);

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        items.Should().HaveCount(1);
        totalCount.Should().Be(1);
        items.First().Slug.Should().Be("orphan-asset");
    }

    [Fact]
    public async Task GetAllAsync_ShouldSearchByTerm_InMultipleFields()
    {
        // Arrange
        var asset1 = new MediaAsset { Id = Guid.NewGuid(), Slug = "sunset-beach", FileName = "img1.jpg", AltTextEn = "Yellow sun" };
        var asset2 = new MediaAsset { Id = Guid.NewGuid(), Slug = "forest", FileName = "img2.jpg", AltTextEn = "Green trees" };
        var asset3 = new MediaAsset { Id = Guid.NewGuid(), Slug = "mountain", FileName = "sunset-top.jpg", AltTextEn = "Snow" };

        _context.MediaAssets.AddRange(asset1, asset2, asset3);
        await _context.SaveChangesAsync();

        var parameters = new MediaQueryParameters(q: "sunset");

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        // Должен найти asset1 (по слагу) и asset3 (по имени файла)
        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
        items.Should().Contain(x => x.Slug == "sunset-beach");
        items.Should().Contain(x => x.Slug == "mountain");
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyPagination()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            _context.MediaAssets.Add(new MediaAsset { Id = Guid.NewGuid(), Slug = $"img-{i:D2}", FileName = $"{i}.jpg" });
        }
        await _context.SaveChangesAsync();

        // Запрашиваем вторую страницу (с 10 по 15 элемент)
        var parameters = new MediaQueryParameters(_start: 10, _end: 20);

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        items.Should().HaveCount(5);
        totalCount.Should().Be(15);
    }
}