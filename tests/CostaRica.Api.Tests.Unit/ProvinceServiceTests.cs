using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Tests.Unit;

public class ProvinceServiceTests
{
    private readonly DirectoryDbContext _context;
    private readonly ProvinceService _service;

    public ProvinceServiceTests()
    {
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new ProvinceService(_context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedItemsAndCorrectTotalCount()
    {
        // Arrange
        _context.Provinces.AddRange(
            new Province { Id = Guid.NewGuid(), Name = "A", Slug = "a" },
            new Province { Id = Guid.NewGuid(), Name = "B", Slug = "b" },
            new Province { Id = Guid.NewGuid(), Name = "C", Slug = "c" }
        );
        await _context.SaveChangesAsync();

        // Act - берем 1-ю страницу размером 2
        var (items, totalCount) = await _service.GetAllAsync(page: 1, pageSize: 2);

        // Assert
        items.Should().HaveCount(2);
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        _context.Provinces.AddRange(
            new Province { Id = Guid.NewGuid(), Name = "San José", Slug = "san-jose" },
            new Province { Id = Guid.NewGuid(), Name = "Alajuela", Slug = "alajuela" }
        );
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _service.GetAllAsync(searchTerm: "jose");

        // Assert
        items.Should().ContainSingle();
        items.First().Name.Should().Be("San José");
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSortBySlugDescending()
    {
        // Arrange
        _context.Provinces.AddRange(
            new Province { Id = Guid.NewGuid(), Name = "Province A", Slug = "aaa" },
            new Province { Id = Guid.NewGuid(), Name = "Province B", Slug = "bbb" }
        );
        await _context.SaveChangesAsync();

        // Act
        var (items, _) = await _service.GetAllAsync(sortBy: "slug", isAscending: false);

        // Assert
        items.First().Slug.Should().Be("bbb");
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnProvince_WhenExists()
    {
        // Arrange
        var province = new Province { Id = Guid.NewGuid(), Name = "San José", Slug = "san-jose" };
        _context.Provinces.Add(province);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBySlugAsync("san-jose");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("San José");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenSlugAlreadyExists()
    {
        // Arrange
        var existing = new Province { Id = Guid.NewGuid(), Name = "Old", Slug = "duplicate" };
        _context.Provinces.Add(existing);
        await _context.SaveChangesAsync();

        var dto = new ProvinceUpsertDto("New", "duplicate");

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTrue_AndApplyChanges()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.Provinces.Add(new Province { Id = id, Name = "Old Name", Slug = "old-slug" });
        await _context.SaveChangesAsync();

        var dto = new ProvinceUpsertDto("New Name", "new-slug");

        // Act
        var result = await _service.UpdateAsync(id, dto);

        // Assert
        result.Should().BeTrue();
        var updated = await _context.Provinces.FindAsync(id);
        updated!.Name.Should().Be("New Name");
        updated.Slug.Should().Be("new-slug");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.Provinces.Add(new Province { Id = id, Name = "To Delete", Slug = "delete-me" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.Should().BeTrue();
        _context.Provinces.Should().BeEmpty();
    }
}