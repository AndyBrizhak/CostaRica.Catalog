using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class ProvinceService(DirectoryDbContext db) : IProvinceService
{
    public async Task<IEnumerable<ProvinceResponseDto>> GetAllAsync(bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();

        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        var provinces = await query.ToListAsync();

        return provinces.Select(p => new ProvinceResponseDto(
            p.Id,
            p.Name,
            p.Slug,
            includeCities
                ? p.Cities.Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId))
                : null
        ));
    }

    public async Task<ProvinceResponseDto?> GetByIdAsync(Guid id, bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();

        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        var province = await query.FirstOrDefaultAsync(p => p.Id == id);

        if (province is null) return null;

        return new ProvinceResponseDto(
            province.Id,
            province.Name,
            province.Slug,
            includeCities
                ? province.Cities.Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId))
                : null
        );
    }

    public async Task<ProvinceResponseDto?> GetBySlugAsync(string slug, bool includeCities = false)
    {
        var query = db.Provinces.AsNoTracking().AsQueryable();

        if (includeCities)
        {
            query = query.Include(p => p.Cities);
        }

        var province = await query.FirstOrDefaultAsync(p => p.Slug == slug);

        if (province is null) return null;

        return new ProvinceResponseDto(
            province.Id,
            province.Name,
            province.Slug,
            includeCities
                ? province.Cities.Select(c => new CityResponseDto(c.Id, c.Name, c.Slug, c.ProvinceId))
                : null
        );
    }

    public async Task<ProvinceResponseDto?> CreateAsync(ProvinceUpsertDto dto)
    {
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
        var province = await db.Provinces.FindAsync(id);
        if (province is null) return false;

        // Помним, что в DbContext настроен DeleteBehavior.Restrict, 
        // так что если города есть, SaveChangesAsync выбросит исключение.
        db.Provinces.Remove(province);
        await db.SaveChangesAsync();
        return true;
    }
}