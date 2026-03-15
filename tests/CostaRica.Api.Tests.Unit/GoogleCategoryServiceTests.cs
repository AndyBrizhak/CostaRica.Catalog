using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Tests.Unit;

public class GoogleCategoryServiceTests
{
    private readonly DirectoryDbContext _context;
    private readonly GoogleCategoryService _service;

    public GoogleCategoryServiceTests()
    {
        // Создаем уникальную БД в памяти для каждого прогона тестов
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new GoogleCategoryService(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCategory_WhenDataIsValid()
    {
        // Arrange
        var dto = new GoogleCategoryUpsertDto("test_gcid", "Test Category", "Categoría de prueba");

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Gcid.Should().Be("test_gcid");

        var inDb = await _context.GoogleCategories.AnyAsync(c => c.Gcid == "test_gcid");
        inDb.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenGcidAlreadyExists()
    {
        // Arrange
        var gcid = "duplicate_gcid";
        _context.GoogleCategories.Add(new GoogleCategory { Id = Guid.NewGuid(), Gcid = gcid, NameEn = "Old", NameEs = "Viejo" });
        await _context.SaveChangesAsync();

        var dto = new GoogleCategoryUpsertDto(gcid, "New", "Nuevo");

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByGcidAsync_ShouldReturnCategory_WhenExists()
    {
        // Arrange
        var gcid = "find_me";
        _context.GoogleCategories.Add(new GoogleCategory { Id = Guid.NewGuid(), Gcid = gcid, NameEn = "Target", NameEs = "Objetivo" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByGcidAsync(gcid);

        // Assert
        result.Should().NotBeNull();
        result!.Gcid.Should().Be(gcid);
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        _context.GoogleCategories.AddRange(
            new GoogleCategory { Id = Guid.NewGuid(), Gcid = "c1", NameEn = "Coffee Shop", NameEs = "Cafetería" },
            new GoogleCategory { Id = Guid.NewGuid(), Gcid = "c2", NameEn = "Restaurant", NameEs = "Restaurante" }
        );
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _service.SearchAsync("coffee");

        // Assert
        totalCount.Should().Be(1);
        items.First().NameEn.Should().Be("Coffee Shop");
    }

    [Fact]
    public async Task BulkImportAsync_ShouldAddOnlyNewCategories()
    {
        // Arrange
        var existingGcid = "already_exists";
        _context.GoogleCategories.Add(new GoogleCategory { Id = Guid.NewGuid(), Gcid = existingGcid, NameEn = "Old", NameEs = "Viejo" });
        await _context.SaveChangesAsync();

        var importData = new List<GoogleCategoryImportDto>
        {
            new(existingGcid, "Duplicate", "Duplicado"), // Должен быть пропущен
            new("new_1", "New Category 1", "Nueva 1"),    // Должен быть добавлен
            new("new_2", "New Category 2", "Nueva 2")     // Должен быть добавлен
        };

        // Act
        var importedCount = await _service.BulkImportAsync(importData);

        // Assert
        importedCount.Should().Be(2);
        var totalInDb = await _context.GoogleCategories.CountAsync();
        totalInDb.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldApplyChanges_WhenCategoryExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.GoogleCategories.Add(new GoogleCategory { Id = id, Gcid = "old_gcid", NameEn = "Old", NameEs = "Old" });
        await _context.SaveChangesAsync();

        var dto = new GoogleCategoryUpsertDto("updated_gcid", "New Name", "Nuevo Nombre");

        // Act
        var result = await _service.UpdateAsync(id, dto);

        // Assert
        result.Should().BeTrue();
        var updated = await _context.GoogleCategories.FindAsync(id);
        updated!.Gcid.Should().Be("updated_gcid");
        updated.NameEn.Should().Be("New Name");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_AndRemoveCategory()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.GoogleCategories.Add(new GoogleCategory { Id = id, Gcid = "to_delete", NameEn = "X", NameEs = "X" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.Should().BeTrue();
        var inDb = await _context.GoogleCategories.FindAsync(id);
        inDb.Should().BeNull();
    }
}