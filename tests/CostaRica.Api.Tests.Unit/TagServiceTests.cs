using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Tests.Unit;

public class TagServiceTests
{
    private readonly DirectoryDbContext _context;
    private readonly TagService _service;

    public TagServiceTests()
    {
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new TagService(_context);
    }

    private async Task<TagGroup> SeedGroupAsync(string name = "Default Group", string slug = "default")
    {
        var group = new TagGroup { Id = Guid.NewGuid(), NameEn = name, NameEs = name, Slug = slug };
        _context.TagGroups.Add(group);
        await _context.SaveChangesAsync();
        return group;
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnTag_WhenGroupExistsAndDataIsValid()
    {
        // Arrange
        var group = await SeedGroupAsync();
        var dto = new TagUpsertDto("WiFi", "WiFi", "wifi", group.Id);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("wifi");
        result.TagGroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenGroupDoesNotExist()
    {
        // Arrange
        var dto = new TagUpsertDto("WiFi", "WiFi", "wifi", Guid.NewGuid()); // Несуществующий ID

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenSlugAlreadyExists()
    {
        // Arrange
        var group = await SeedGroupAsync();
        _context.Tags.Add(new Tag { Id = Guid.NewGuid(), NameEn = "Old", NameEs = "O", Slug = "dup", TagGroupId = group.Id });
        await _context.SaveChangesAsync();

        var dto = new TagUpsertDto("New", "N", "dup", group.Id);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByGroupIdAsync_ShouldReturnOnlyRelevantTags()
    {
        // Arrange
        var g1 = await SeedGroupAsync("G1", "g1");
        var g2 = await SeedGroupAsync("G2", "g2");

        _context.Tags.Add(new Tag { Id = Guid.NewGuid(), NameEn = "T1", Slug = "t1", TagGroupId = g1.Id });
        _context.Tags.Add(new Tag { Id = Guid.NewGuid(), NameEn = "T2", Slug = "t2", TagGroupId = g2.Id });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByGroupIdAsync(g1.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().Slug.Should().Be("t1");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenMovingToNonExistentGroup()
    {
        // Arrange
        var group = await SeedGroupAsync();
        var tag = new Tag { Id = Guid.NewGuid(), NameEn = "T", Slug = "t", TagGroupId = group.Id };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        var updateDto = new TagUpsertDto("T", "T", "t", Guid.NewGuid()); // Смена на битый ID группы

        // Act
        var result = await _service.UpdateAsync(tag.Id, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var group = await SeedGroupAsync();
        _context.Tags.Add(new Tag { Id = Guid.NewGuid(), NameEn = "WiFi", Slug = "wifi-spot", TagGroupId = group.Id });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBySlugAsync("WIFI-SPOT");

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("wifi-spot");
    }
}