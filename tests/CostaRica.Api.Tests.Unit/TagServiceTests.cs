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
        // Изоляция базы данных для каждого теста
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
    public async Task GetAllAsync_ShouldFilterByTagGroupId()
    {
        // Arrange
        var group1 = await SeedGroupAsync("Group 1", "g1");
        var group2 = await SeedGroupAsync("Group 2", "g2");

        _context.Tags.AddRange(
            new Tag { Id = Guid.NewGuid(), NameEn = "Tag 1", NameEs = "T1", Slug = "t1", TagGroupId = group1.Id },
            new Tag { Id = Guid.NewGuid(), NameEn = "Tag 2", NameEs = "T2", Slug = "t2", TagGroupId = group1.Id },
            new Tag { Id = Guid.NewGuid(), NameEn = "Tag 3", NameEs = "T3", Slug = "t3", TagGroupId = group2.Id }
        );
        await _context.SaveChangesAsync();

        var parameters = new TagQueryParameters(TagGroupId: group1.Id);

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(t => t.TagGroupId.Should().Be(group1.Id));
    }

    [Fact]
    public async Task GetAllAsync_WithGlobalSearch_ShouldFindTagsAcrossFields()
    {
        // Arrange
        var group = await SeedGroupAsync();
        _context.Tags.AddRange(
            new Tag { Id = Guid.NewGuid(), NameEn = "Free WiFi", NameEs = "WiFi gratis", Slug = "wifi", TagGroupId = group.Id },
            new Tag { Id = Guid.NewGuid(), NameEn = "Parking", NameEs = "Parqueo", Slug = "parking", TagGroupId = group.Id }
        );
        await _context.SaveChangesAsync();

        var parameters = new TagQueryParameters(Q: "gratis"); // Поиск по испанскому полю

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        totalCount.Should().Be(1);
        items.Should().ContainSingle(t => t.Slug == "wifi");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenTagGroupDoesNotExist()
    {
        // Arrange
        var dto = new TagUpsertDto("Test", "Test", "test", Guid.NewGuid()); // Несуществующий ID группы

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
        _context.Tags.Add(new Tag { Id = Guid.NewGuid(), NameEn = "Old", NameEs = "O", Slug = "existing-tag", TagGroupId = group.Id });
        await _context.SaveChangesAsync();

        var dto = new TagUpsertDto("New", "N", "EXISTING-TAG", group.Id);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreventDuplicateSlugsOnOtherTags()
    {
        // Arrange
        var group = await SeedGroupAsync();
        var tag1 = new Tag { Id = Guid.NewGuid(), NameEn = "T1", NameEs = "T1", Slug = "slug1", TagGroupId = group.Id };
        var tag2 = new Tag { Id = Guid.NewGuid(), NameEn = "T2", NameEs = "T2", Slug = "slug2", TagGroupId = group.Id };
        _context.Tags.AddRange(tag1, tag2);
        await _context.SaveChangesAsync();

        var updateDto = new TagUpsertDto("Updated", "U", "slug2", group.Id); // Пытаемся занять слаг второго тега

        // Act
        var result = await _service.UpdateAsync(tag1.Id, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyPaginationCorrectly()
    {
        // Arrange
        var group = await SeedGroupAsync();
        for (int i = 1; i <= 5; i++)
        {
            _context.Tags.Add(new Tag { Id = Guid.NewGuid(), NameEn = $"Tag {i}", NameEs = $"T{i}", Slug = $"t{i}", TagGroupId = group.Id });
        }
        await _context.SaveChangesAsync();

        // Просим 2 элемента, пропуская первый
        var parameters = new TagQueryParameters(_start: 1, _end: 3);

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
    }
}