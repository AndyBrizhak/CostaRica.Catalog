using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using CostaRica.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CostaRica.Api.Tests.Unit;

public class ProvinceServiceTests
{
    private readonly DirectoryDbContext _mockContext;
    private readonly ProvinceService _service;

    public ProvinceServiceTests()
    {
        // Создаем "пустые" настройки для контекста
        var options = new DbContextOptionsBuilder<DirectoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // В случае с DbContext часто удобнее использовать InMemory провайдер 
        // для юнит-тестов вместо полного мокирования NSubstitute, 
        // так как это избавляет от ручной настройки DbSet.
        _mockContext = new DirectoryDbContext(options);
        _service = new ProvinceService(_mockContext);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnDto_WhenSlugIsUnique()
    {
        // Arrange (Подготовка)
        var dto = new ProvinceUpsertDto("Guanacaste", "guanacaste");

        // Act (Действие)
        var result = await _service.CreateAsync(dto);

        // Assert (Проверка)
        result.Should().NotBeNull();
        result!.Name.Should().Be(dto.Name);
        result.Slug.Should().Be(dto.Slug);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNull_WhenSlugAlreadyExists()
    {
        // Arrange
        var existingProvince = new Province
        {
            Id = Guid.NewGuid(),
            Name = "Existing",
            Slug = "conflict"
        };
        _mockContext.Provinces.Add(existingProvince);
        await _mockContext.SaveChangesAsync();

        var dto = new ProvinceUpsertDto("New Name", "conflict");

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProvinceDoesNotExist()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
}