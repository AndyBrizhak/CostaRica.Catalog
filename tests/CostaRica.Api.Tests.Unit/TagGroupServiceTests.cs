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
        // Настройка уникальной БД для каждого теста
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new TagGroupService(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnTagGroup_WhenDataIsValid()
    {
        // Arrange
        var dto = new TagGroupUpsertDto("Amenities", "Comodidades", "amenities");

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.NameEn.Should().Be("Amenities");
        result.Slug.Should().Be("amenities");

        var inDb = await _context.TagGroups.FirstOrDefaultAsync(x => x.Slug == "amenities");
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenSlugAlreadyExists()
    {
        // Arrange
        var existing = new TagGroup { Id = Guid.NewGuid(), NameEn = "Old", NameEs = "Viejo", Slug = "duplicate" };
        _context.TagGroups.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new TagGroupUpsertDto("New", "Nuevo", "duplicate");

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var tg = new TagGroup { Id = Guid.NewGuid(), NameEn = "Test", NameEs = "Test", Slug = "test-slug" };
        _context.TagGroups.Add(tg);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBySlugAsync("TEST-SLUG");

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be("test-slug");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenNewSlugIsTakenByAnotherGroup()
    {
        // Arrange
        var group1 = new TagGroup { Id = Guid.NewGuid(), NameEn = "G1", NameEs = "G1", Slug = "slug1" };
        var group2 = new TagGroup { Id = Guid.NewGuid(), NameEn = "G2", NameEs = "G2", Slug = "slug2" };
        _context.TagGroups.AddRange(group1, group2);
        await _context.SaveChangesAsync();

        var updateDto = new TagGroupUpsertDto("Updated", "Actualizado", "slug2"); // пытаемся занять слаг второй группы

        // Act
        var result = await _service.UpdateAsync(group1.Id, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenGroupDoesNotExist()
    {
        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }
}