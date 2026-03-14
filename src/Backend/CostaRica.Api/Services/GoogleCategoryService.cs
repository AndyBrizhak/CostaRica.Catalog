using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class GoogleCategoryService(DirectoryDbContext db) : IGoogleCategoryService
{
    public async Task<IEnumerable<GoogleCategoryResponseDto>> GetAllAsync()
    {
        var categories = await db.GoogleCategories
            .AsNoTracking()
            .OrderBy(c => c.NameEn)
            .ToListAsync();

        return categories.Select(MapToDto);
    }

    public async Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id)
    {
        var category = await db.GoogleCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return category is null ? null : MapToDto(category);
    }

    public async Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid)
    {
        var category = await db.GoogleCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Gcid == gcid);

        return category is null ? null : MapToDto(category);
    }

    public async Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto)
    {
        // Проверка на уникальность Gcid (например, "restaurant")
        var exists = await db.GoogleCategories.AnyAsync(c => c.Gcid == dto.Gcid);
        if (exists) return null;

        var category = new GoogleCategory
        {
            Id = Guid.NewGuid(),
            Gcid = dto.Gcid,
            NameEn = dto.NameEn,
            NameEs = dto.NameEs
        };

        db.GoogleCategories.Add(category);
        await db.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<bool> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto)
    {
        var category = await db.GoogleCategories.FindAsync(id);
        if (category is null) return false;

        category.Gcid = dto.Gcid;
        category.NameEn = dto.NameEn;
        category.NameEs = dto.NameEs;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await db.GoogleCategories.FindAsync(id);
        if (category is null) return false;

        db.GoogleCategories.Remove(category);
        await db.SaveChangesAsync();
        return true;
    }

    private static GoogleCategoryResponseDto MapToDto(GoogleCategory c)
        => new(c.Id, c.Gcid, c.NameEn, c.NameEs);
}