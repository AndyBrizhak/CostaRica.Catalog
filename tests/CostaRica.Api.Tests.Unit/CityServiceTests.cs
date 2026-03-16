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
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DirectoryDbContext(options);
        _service = new CityService(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenProvinceDoesNotExist()
    {
        // Arrange
        var dto = new CityUpsertDto("Liberia", "liberia", Guid.NewGuid());

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCityWithProvinceName_WhenDataIsValid()
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
        result!.ProvinceName.Should().Be("Guanacaste");
        result.Name.Should().Be("Liberia");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedItemsAndCorrectTotalCount()
    {
        // Arrange
        var province = new Province { Id = Guid.NewGuid(), Name = "P1", Slug = "p1" };
        _context.Provinces.Add(province);

        for (int i = 1; i <= 5; i++)
        {
            _context.Cities.Add(new City { Id = Guid.NewGuid(), Name = $"City {i}", Slug = $"c{i}", ProvinceId = province.Id });
        }
        await _context.SaveChangesAsync();

        // Запрашиваем первую страницу из 2 элементов
        var parameters = new CityQueryParameters(_start: 0, _end: 2);

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        items.Should().HaveCount(2);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByProvinceId()
    {
        // Arrange
        var p1 = new Province { Id = Guid.NewGuid(), Name = "P1", Slug = "p1" };
        var p2 = new Province { Id = Guid.NewGuid(), Name = "P2", Slug = "p2" };
        _context.Provinces.AddRange(p1, p2);

        _context.Cities.Add(new City { Id = Guid.NewGuid(), Name = "City 1", Slug = "c1", ProvinceId = p1.Id });
        _context.Cities.Add(new City { Id = Guid.NewGuid(), Name = "City 2", Slug = "c2", ProvinceId = p2.Id });
        await _context.SaveChangesAsync();

        var parameters = new CityQueryParameters(ProvinceId: p1.Id);

        // Act
        var (items, totalCount) = await _service.GetAllAsync(parameters);

        // Assert
        items.Should().HaveCount(1);
        items.First().ProvinceId.Should().Be(p1.Id);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSortByNameDescending()
    {
        // Arrange
        var p = new Province { Id = Guid.NewGuid(), Name = "P", Slug = "p" };
        _context.Provinces.Add(p);
        _context.Cities.Add(new City { Id = Guid.NewGuid(), Name = "A-City", Slug = "a", ProvinceId = p.Id });
        _context.Cities.Add(new City { Id = Guid.NewGuid(), Name = "Z-City", Slug = "z", ProvinceId = p.Id });
        await _context.SaveChangesAsync();

        var parameters = new CityQueryParameters(_sort: "Name", _order: "DESC");

        // Act
        var (items, _) = await _service.GetAllAsync(parameters);

        // Assert
        items.First().Name.Should().Be("Z-City");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenSlugAlreadyExists()
    {
        // Arrange
        var p = new Province { Id = Guid.NewGuid(), Name = "P", Slug = "p" };
        _context.Provinces.Add(p);

        var city1 = new City { Id = Guid.NewGuid(), Name = "City 1", Slug = "c1", ProvinceId = p.Id };
        var city2 = new City { Id = Guid.NewGuid(), Name = "City 2", Slug = "c2", ProvinceId = p.Id };

        _context.Cities.AddRange(city1, city2);
        await _context.SaveChangesAsync();

        // Пытаемся обновить city1, присвоив ему слаг от city2
        var updateDto = new CityUpsertDto("New Name", "c2", p.Id);

        // Act
        var result = await _service.UpdateAsync(city1.Id, updateDto);

        // Assert
        result.Should().BeFalse();
    }
}