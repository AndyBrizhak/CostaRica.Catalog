using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Tests.Unit;

public class CityServiceTests
{
    private readonly DirectoryDbContext _context;
    private readonly CityService _service;

    public CityServiceTests()
    {
        // Используем InMemory базу для изоляции тестов
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new CityService(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenProvinceDoesNotExist()
    {
        // Arrange: Провинции с таким ID нет в базе
        var dto = new CityUpsertDto("Liberia", "liberia", Guid.NewGuid());

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCity_WhenDataIsValid()
    {
        // Arrange
        var province = new Province { Id = Guid.NewGuid(), Name = "Guanacaste", Slug = "guanacaste" };
        _context.Provinces.Add(province);
        await _context.SaveChangesAsync();

        var dto = new CityUpsertDto("Liberia", "liberia", province.Id);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Liberia");
        result.ProvinceId.Should().Be(province.Id);
    }

    [Fact]
    public async Task GetByProvinceAsync_ShouldReturnOnlyCitiesOfThatProvince()
    {
        // Arrange
        var p1 = new Province { Id = Guid.NewGuid(), Name = "P1", Slug = "slug1" };
        var p2 = new Province { Id = Guid.NewGuid(), Name = "P2", Slug = "slug2" };
        _context.Provinces.AddRange(p1, p2);

        _context.Cities.Add(new City { Id = Guid.NewGuid(), Name = "City1", Slug = "c1", ProvinceId = p1.Id });
        _context.Cities.Add(new City { Id = Guid.NewGuid(), Name = "City2", Slug = "c2", ProvinceId = p2.Id });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByProvinceAsync("slug1");

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("City1");
    }
}