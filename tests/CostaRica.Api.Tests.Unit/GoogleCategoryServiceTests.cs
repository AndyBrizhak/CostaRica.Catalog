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
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new GoogleCategoryService(_context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedData_WithTotalCount()
    {
        // Arrange
        _context.GoogleCategories.AddRange(new List<GoogleCategory>
        {
            new() { Id = Guid.NewGuid(), Gcid = "cat1", NameEn = "A", NameEs = "A" },
            new() { Id = Guid.NewGuid(), Gcid = "cat2", NameEn = "B", NameEs = "B" },
            new() { Id = Guid.NewGuid(), Gcid = "cat3", NameEn = "C", NameEs = "C" }
        });
        await _context.SaveChangesAsync();

        var parameters = new GoogleCategoryQueryParameters(_start: 0, _end: 2, _sort: "NameEn", _order: "ASC");

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        totalCount.Should().Be(3);
        items.Should().HaveCount(2);
        items.First().NameEn.Should().Be("A");
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterBySearchTerm_InMultipleFields()
    {
        // Arrange
        _context.GoogleCategories.Add(new GoogleCategory { Id = Guid.NewGuid(), Gcid = "unique_gcid", NameEn = "English", NameEs = "Spanish" });
        await _context.SaveChangesAsync();

        // Поиск по GCID
        var res1 = await _service.GetAllAsync(new GoogleCategoryQueryParameters(q: "unique"));
        // Поиск по английскому имени
        var res2 = await _service.GetAllAsync(new GoogleCategoryQueryParameters(q: "English"));

        // Assert
        res1.Items.Should().NotBeEmpty();
        res2.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldSupportGetManyByIds()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _context.GoogleCategories.AddRange(new List<GoogleCategory>
        {
            new() { Id = id1, Gcid = "c1", NameEn = "C1", NameEs = "C1" },
            new() { Id = id2, Gcid = "c2", NameEn = "C2", NameEs = "C2" },
            new() { Id = Guid.NewGuid(), Gcid = "c3", NameEn = "C3", NameEs = "C3" }
        });
        await _context.SaveChangesAsync();

        var parameters = new GoogleCategoryQueryParameters(id: [id1, id2]);

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        items.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenGcidAlreadyExists()
    {
        // Arrange
        var existingGcid = "duplicate_me";
        _context.GoogleCategories.Add(new GoogleCategory { Id = Guid.NewGuid(), Gcid = existingGcid, NameEn = "X", NameEs = "X" });
        await _context.SaveChangesAsync();

        var dto = new GoogleCategoryUpsertDto(existingGcid, "New", "Nuevo");

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task BulkImportAsync_ShouldIgnoreDuplicatesWithinList()
    {
        // Arrange
        var importData = new List<GoogleCategoryImportDto>
        {
            new("same", "Name", "Nombre"),
            new("same", "Name", "Nombre"),
            new("different", "Other", "Otro")
        };

        // Act
        var count = await _service.BulkImportAsync(importData);

        // Assert
        count.Should().Be(2); // Только "same" (один раз) и "different"
        var inDb = await _context.GoogleCategories.CountAsync();
        inDb.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenGcidIsTakenByAnotherCategory()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _context.GoogleCategories.AddRange(new List<GoogleCategory>
        {
            new() { Id = id1, Gcid = "cat1", NameEn = "1", NameEs = "1" },
            new() { Id = id2, Gcid = "cat2", NameEn = "2", NameEs = "2" }
        });
        await _context.SaveChangesAsync();

        // Пытаемся обновить cat1, установив ему GCID от cat2
        var updateDto = new GoogleCategoryUpsertDto("cat2", "Updated", "Actualizado");

        // Act
        var result = await _service.UpdateAsync(id1, updateDto);

        // Assert
        result.Should().BeFalse();
    }
}