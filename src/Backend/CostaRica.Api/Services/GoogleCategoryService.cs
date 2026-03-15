using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class GoogleCategoryService(DirectoryDbContext db) : IGoogleCategoryService
{
    public async Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id)
    {
        var category = await db.GoogleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return category is null ? null : MapToDto(category);
    }

    public async Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid)
    {
        var category = await db.GoogleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Gcid == gcid);
        return category is null ? null : MapToDto(category);
    }

    public async Task<(IEnumerable<GoogleCategoryResponseDto> Items, int TotalCount)> SearchAsync(
        string? searchTerm, int page = 1, int pageSize = 20, string? sortBy = "NameEn", bool isAscending = true)
    {
        var query = db.GoogleCategories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerTerm = searchTerm.ToLower();
            query = query.Where(c =>
                c.NameEn.ToLower().Contains(lowerTerm) ||
                c.NameEs.ToLower().Contains(lowerTerm) ||
                c.Gcid.ToLower().Contains(lowerTerm));
        }

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "namees" => isAscending ? query.OrderBy(c => c.NameEs) : query.OrderByDescending(c => c.NameEs),
            "gcid" => isAscending ? query.OrderBy(c => c.Gcid) : query.OrderByDescending(c => c.Gcid),
            _ => isAscending ? query.OrderBy(c => c.NameEn) : query.OrderByDescending(c => c.NameEn)
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(MapToDto), totalCount);
    }

    public async Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto)
    {
        var exists = await db.GoogleCategories.AnyAsync(c => c.Gcid == dto.Gcid);
        if (exists) return null;

        var category = new GoogleCategory { Id = Guid.NewGuid(), Gcid = dto.Gcid, NameEn = dto.NameEn, NameEs = dto.NameEs };
        db.GoogleCategories.Add(category);
        await db.SaveChangesAsync();
        return MapToDto(category);
    }

    // РЕАЛИЗАЦИЯ МАССОВОГО ИМПОРТА
    public async Task<int> BulkImportAsync(IEnumerable<GoogleCategoryImportDto> categories)
    {
        // 1. Получаем все существующие GCID из базы для быстрой проверки
        var existingGcids = await db.GoogleCategories
            .Select(c => c.Gcid)
            .ToHashSetAsync();

        // 2. Оставляем только те категории из списка, которых еще нет в базе
        var newCategories = categories
            .Where(c => !existingGcids.Contains(c.Gcid))
            .Select(c => new GoogleCategory
            {
                Id = Guid.NewGuid(),
                Gcid = c.Gcid,
                NameEn = c.NameEn,
                NameEs = c.NameEs
            })
            .ToList();

        if (newCategories.Count == 0) return 0;

        // 3. Массовое добавление
        db.GoogleCategories.AddRange(newCategories);
        return await db.SaveChangesAsync();
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

    private static GoogleCategoryResponseDto MapToDto(GoogleCategory c) => new(c.Id, c.Gcid, c.NameEn, c.NameEs);
}