using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

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

        return province is null ? null : new ProvinceResponseDto(province.Id, province.Name, province.Slug);
    }

    public async Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto)
    {
        // Проверяем наличие дубликата по уникальному индексу Slug
        var exists = await db.Provinces.AnyAsync(p => p.Slug == dto.Slug);
        if (exists) return null;

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