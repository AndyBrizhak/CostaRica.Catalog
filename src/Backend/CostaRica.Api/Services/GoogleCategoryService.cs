using CostaRica.Api.Data;
using CostaRica.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CostaRica.Api.Services;

public class GoogleCategoryService(DirectoryDbContext db) : IGoogleCategoryService
{
    public async Task<(IEnumerable<GoogleCategoryResponseDto> Items, int TotalCount)> GetAllAsync(
        GoogleCategoryQueryParameters parameters,
        CancellationToken ct = default)
    {
        var query = db.GoogleCategories.AsNoTracking().AsQueryable();

        if (parameters.id is { Length: > 0 })
        {
            query = query.Where(c => parameters.id.Contains(c.Id));
        }

        if (!string.IsNullOrWhiteSpace(parameters.q))
        {
            var lowerQ = parameters.q.ToLower();
            query = query.Where(c =>
                c.NameEn.ToLower().Contains(lowerQ) ||
                c.NameEs.ToLower().Contains(lowerQ) ||
                c.Gcid.ToLower().Contains(lowerQ));
        }

        var totalCount = await query.CountAsync(ct);

        // Sorting
        query = parameters._order?.ToUpper() == "DESC"
            ? query.OrderByDescending(c => EF.Property<object>(c, parameters._sort ?? "NameEn"))
            : query.OrderBy(c => EF.Property<object>(c, parameters._sort ?? "NameEn"));

        // Pagination
        if (parameters._start.HasValue && parameters._end.HasValue)
        {
            var take = parameters._end.Value - parameters._start.Value;
            query = query.Skip(parameters._start.Value).Take(take);
        }

        var items = await query
            .Select(c => new GoogleCategoryResponseDto(c.Id, c.Gcid, c.NameEn, c.NameEs))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<GoogleCategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return category is null ? null : new GoogleCategoryResponseDto(category.Id, category.Gcid, category.NameEn, category.NameEs);
    }

    public async Task<GoogleCategoryResponseDto?> GetByGcidAsync(string gcid, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Gcid == gcid, ct);
        return category is null ? null : new GoogleCategoryResponseDto(category.Id, category.Gcid, category.NameEn, category.NameEs);
    }

    public async Task<GoogleCategoryResponseDto?> CreateAsync(GoogleCategoryUpsertDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Gcid)) return null;

        if (await db.GoogleCategories.AnyAsync(c => c.Gcid == dto.Gcid, ct))
            return null;

        var category = new GoogleCategory
        {
            Id = Guid.NewGuid(),
            Gcid = dto.Gcid.Trim(),
            NameEn = dto.NameEn.Trim(),
            NameEs = dto.NameEs.Trim()
        };

        db.GoogleCategories.Add(category);
        await db.SaveChangesAsync(ct);

        return new GoogleCategoryResponseDto(category.Id, category.Gcid, category.NameEn, category.NameEs);
    }

    public async Task<int> BulkImportAsync(IEnumerable<GoogleCategoryImportDto> categories, CancellationToken ct = default)
    {
        var gcids = categories.Select(c => c.Gcid).ToList();
        var existingGcids = await db.GoogleCategories
            .Where(c => gcids.Contains(c.Gcid))
            .Select(c => c.Gcid)
            .ToListAsync(ct);

        var newCategories = categories
            .Where(c => !existingGcids.Contains(c.Gcid))
            .Select(c => new GoogleCategory
            {
                Id = Guid.NewGuid(),
                Gcid = c.Gcid.Trim(),
                NameEn = c.NameEn.Trim(),
                NameEs = c.NameEs.Trim()
            })
            .ToList();

        if (newCategories.Count == 0) return 0;

        db.GoogleCategories.AddRange(newCategories);
        return await db.SaveChangesAsync(ct);
    }

    public async Task<GoogleCategoryUpdateResult> UpdateAsync(Guid id, GoogleCategoryUpsertDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Gcid) || string.IsNullOrWhiteSpace(dto.NameEn))
            return GoogleCategoryUpdateResult.Conflict; // Or handle as validation error

        var category = await db.GoogleCategories.FindAsync([id], ct);
        if (category is null) return GoogleCategoryUpdateResult.NotFound;

        // Check if new GCID is already taken by another entity
        var gcidConflict = await db.GoogleCategories
            .AnyAsync(c => c.Gcid == dto.Gcid && c.Id != id, ct);

        if (gcidConflict) return GoogleCategoryUpdateResult.Conflict;

        category.Gcid = dto.Gcid.Trim();
        category.NameEn = dto.NameEn.Trim();
        category.NameEs = dto.NameEs.Trim();

        await db.SaveChangesAsync(ct);
        return GoogleCategoryUpdateResult.Success;
    }

    public async Task<(GoogleCategoryDeleteResult Result, int UsageCount)> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.GoogleCategories.FindAsync([id], ct);
        if (category is null) return (GoogleCategoryDeleteResult.NotFound, 0);

        // Check dependencies in BusinessPages (both Primary and Secondary)
        var usageCount = await db.BusinessPages
            .CountAsync(bp => bp.PrimaryCategoryId == id || bp.SecondaryCategories.Any(sc => sc.Id == id), ct);

        if (usageCount > 0)
            return (GoogleCategoryDeleteResult.InUse, usageCount);

        db.GoogleCategories.Remove(category);
        await db.SaveChangesAsync(ct);

        return (GoogleCategoryDeleteResult.Success, 0);
    }
}