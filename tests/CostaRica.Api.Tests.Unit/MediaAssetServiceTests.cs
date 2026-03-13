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
            ProvinceId = Guid.NewGuid(), // Обязательное поле согласно модели
            Location = new Point(0, 0) { SRID = 4326 }
        };

        asset.BusinessPages.Add(businessPage);

        _context.MediaAssets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(assetId);

        // Assert
        result.Success.Should().BeFalse();
        // Исправлено: ожидаемый текст приведен в соответствие с MediaAssetService.cs
        result.ErrorMessage.Should().Contain("используется в");
        result.ErrorMessage.Should().Contain("бизнес-страницах");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFromDbAndStorage_WhenNoLinksExist()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var fileName = "delete-me.jpg";
        var asset = new MediaAsset { Id = assetId, Slug = "orphan", FileName = fileName };

        _context.MediaAssets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(assetId);

        // Assert
        result.Success.Should().BeTrue();

        var inDb = await _context.MediaAssets.FindAsync(assetId);
        inDb.Should().BeNull();

        await _storageService.Received(1).DeleteAsync(fileName);
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldReturnOnlyOrphans_WhenOnlyOrphansIsTrue()
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

        var orphan = new MediaAsset { Id = Guid.NewGuid(), Slug = "orphan", FileName = "2.jpg" };

        _context.MediaAssets.AddRange(linked, orphan);
        await _context.SaveChangesAsync();

        var filter = new MediaFilterDto { OnlyOrphans = true };

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        result.Should().HaveCount(1);
        result.First().Slug.Should().Be("orphan");
    }
}