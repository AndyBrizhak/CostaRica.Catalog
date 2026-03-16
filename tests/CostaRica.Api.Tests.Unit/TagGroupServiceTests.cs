using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Tests.Unit;

public class TagGroupServiceTests
{
    private readonly DirectoryDbContext _context;
    private readonly TagGroupService _service;

    public TagGroupServiceTests()
    {
        // Настройка уникальной БД для каждого теста (стандарт изоляции)
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new TagGroupService(_context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedAndSortedItems()
    {
        // Arrange
        _context.TagGroups.AddRange(
            new TagGroup { Id = Guid.NewGuid(), NameEn = "B-Group", NameEs = "B", Slug = "b-group" },
            new TagGroup { Id = Guid.NewGuid(), NameEn = "A-Group", NameEs = "A", Slug = "a-group" },
            new TagGroup { Id = Guid.NewGuid(), NameEn = "C-Group", NameEs = "C", Slug = "c-group" }
        );
        await _context.SaveChangesAsync();

        // Просим первые 2 элемента, отсортированные по NameEn (ASC)
        var parameters = new TagGroupQueryParameters(_start: 0, _end: 2, _sort: "NameEn", _order: "ASC");

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        totalCount.Should().Be(3);
        items.Should().HaveCount(2);
        items.First().NameEn.Should().Be("A-Group");
    }

    [Fact]
    public async Task GetAllAsync_WithGlobalSearch_ShouldFilterByAnyTextField()
    {
        // Arrange
        _context.TagGroups.AddRange(
            new TagGroup { Id = Guid.NewGuid(), NameEn = "Amenities", NameEs = "Servicios", Slug = "amenities" },
            new TagGroup { Id = Guid.NewGuid(), NameEn = "Location", NameEs = "Ubicacion", Slug = "location" }
        );
        await _context.SaveChangesAsync();

        var parameters = new TagGroupQueryParameters(Q: "servi"); // Поиск по испанскому названию

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        totalCount.Should().Be(1);
        items.Should().ContainSingle(x => x.Slug == "amenities");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSlug_ShouldReturnNull()
    {
        // Arrange
        var existing = new TagGroup { Id = Guid.NewGuid(), NameEn = "Existing", NameEs = "E", Slug = "duplicate" };
        _context.TagGroups.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new TagGroupUpsertDto("New", "N", "DUPLICATE"); // Проверка регистронезависимости

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenGroupHasTags_ShouldReturnFalseAndNotDelete()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var group = new TagGroup { Id = groupId, NameEn = "Parent", NameEs = "P", Slug = "parent" };
        var tag = new Tag { Id = Guid.NewGuid(), NameEn = "Child", NameEs = "C", Slug = "child", TagGroupId = groupId };

        _context.TagGroups.Add(group);
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(groupId);

        // Assert
        result.Should().BeFalse(); // Удаление запрещено, так как есть зависимости
        var inDb = await _context.TagGroups.FindAsync(groupId);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldHandleSlugConflictCorrectly()
    {
        // Arrange
        var group1 = new TagGroup { Id = Guid.NewGuid(), NameEn = "G1", NameEs = "G1", Slug = "slug1" };
        var group2 = new TagGroup { Id = Guid.NewGuid(), NameEn = "G2", NameEs = "G2", Slug = "slug2" };
        _context.TagGroups.AddRange(group1, group2);
        await _context.SaveChangesAsync();

        var updateDto = new TagGroupUpsertDto("Updated", "U", "slug2"); // Пытаемся занять чужой слаг

        // Act
        var result = await _service.UpdateAsync(group1.Id, updateDto);

        // Assert
        result.Should().BeNull();
    }
}