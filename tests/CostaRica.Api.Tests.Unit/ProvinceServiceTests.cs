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
        // Создаем чистую БД для каждого теста
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new ProvinceService(_context);
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
        result.Cities.Should().BeNull(); // По умолчанию города не подгружаются
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldIncludeCities_WhenRequested()
    {
        // Arrange
        var provinceId = Guid.NewGuid();
        var province = new Province { Id = provinceId, Name = "Alajuela", Slug = "alajuela" };
        var city = new City { Id = Guid.NewGuid(), Name = "Alajuela City", Slug = "alajuela-city", ProvinceId = provinceId };

        _context.Provinces.Add(province);
        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBySlugAsync("alajuela", includeCities: true);

        // Assert
        result.Should().NotBeNull();
        result!.Cities.Should().NotBeNull();
        result.Cities.Should().HaveCount(1);
        result.Cities!.First().Slug.Should().Be("alajuela-city");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoProvincesExist()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
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
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotFound()
    {
        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }
}