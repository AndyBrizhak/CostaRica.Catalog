using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

/// <summary>
/// Реализация сервиса для работы с провинциями.
/// </summary>
public class ProvinceService(DirectoryDbContext db) : IProvinceService
{
    public async Task<IEnumerable<ProvinceResponseDto>> GetAllAsync()
    {
        return await db.Provinces
            .AsNoTracking()
            .Select(p => new ProvinceResponseDto(p.Id, p.Name, p.Slug))
            .ToListAsync();
    }

    public async Task<ProvinceResponseDto?> GetByIdAsync(Guid id)
    {
        var province = await db.Provinces
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (province is null) return null;

        return new ProvinceResponseDto(province.Id, province.Name, province.Slug);
    }

    public async Task<ProvinceResponseDto> CreateAsync(ProvinceUpsertDto dto)
    {
        var province = new Province
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Slug
        };

        db.Provinces.Add(province);
        await db.SaveChangesAsync();

        return new ProvinceResponseDto(province.Id, province.Name, province.Slug);
    }

    public async Task<bool> UpdateAsync(Guid id, ProvinceUpsertDto dto)
    {
        var province = await db.Provinces.FindAsync(id);

        if (province is null) return false;

        province.Name = dto.Name;
        province.Slug = dto.Slug;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await db.Provinces
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();

        return affected > 0;
    }
}